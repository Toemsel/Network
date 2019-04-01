using Network.Async;
using Network.Attributes;
using Network.Converter;
using Network.Enums;
using Network.Extensions;
using Network.Interfaces;
using Network.Packets;
using Network.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// Provides the basic methods that all <see cref="Connection"/> inheritors must implement. It ensures connectivity and
    /// keeps tracks of statistics such as latency. Multi-threaded with 3 separate threads per connection. After calling the
    /// <see cref="Close"/> method, every queued <see cref="Packet"/> will be sent before the <see cref="Connection"/> is fully
    /// closed.
    /// </summary>
    /// <remarks>
    /// Every <see cref="Connection"/> instance has 3 threads:
    /// - (1) Read thread   -> Reads <see cref="Packet"/> objects from the network.
    /// - (2) Invoke thread -> Delegates the handling of received packets to the registered <see cref="PacketReceivedHandler{P}"/>.
    /// - (3) Send thread   -> Writes queued <see cref="Packet"/> objects to the network.
    /// </remarks>
    public abstract partial class Connection : IPacketHandler
    {
        #region Variables

        /// <summary>
        /// The time interval in milliseconds between ping packets.
        /// </summary>
        protected const int PingInterval = 10000;

        /// <summary>
        /// A fixed hashcode that persists with the <see cref="Connection"/> instance for its entire lifetime.
        /// </summary>
        private readonly int hashCode;

        #region Ping Variables

        /// <summary>
        /// True if this instance should send in a specific interval a keep alive packet, to ensure whether there is a connection
        /// or not. If set to [false] <see cref="RTT"/> and <see cref="Ping"/> wont be enabled/refreshed.
        /// </summary>
        /// <remarks>
        /// Defaults to [false] (default value of a <see cref="bool"/>).
        /// </remarks>
        private bool keepAlive;

        /// <summary>
        /// Stopwatch to keep track of when to send out a new <see cref="PingRequest"/>.
        /// </summary>
        private readonly Stopwatch nextPingStopWatch = new Stopwatch();

        /// <summary>
        /// Stopwatch measuring elapsed time since the last <see cref="PingRequest"/> was sent to measure the RTT and ping to
        /// the remote <see cref="Connection"/>.
        /// </summary>
        private readonly Stopwatch currentPingStopWatch = new Stopwatch();

        #endregion Ping Variables

        #region Packet Variables

        /// <summary>
        /// The packet converter used to serialise and deserialise outgoing and incoming packets.
        /// </summary>
        private IPacketConverter packetConverter = new PacketConverter();

        /// <summary>
        /// Holds all the <see cref="UdpConnection"/>s that are currently pending connection to this <see cref="Connection"/>.
        /// </summary>
        private readonly ConcurrentQueue<UdpConnection> pendingUDPConnections = new ConcurrentQueue<UdpConnection>();

        /// <summary>
        /// Holds all received <see cref="Packet"/>s with a known <see cref="PacketReceivedHandler{P}"/> that are yet to be
        /// handled.
        /// </summary>
        private readonly ConcurrentQueue<Packet> receivedPackets = new ConcurrentQueue<Packet>();

        /// <summary>
        /// Holds all received <see cref="Packet"/>s without a known <see cref="PacketReceivedHandler{P}"/> that are yet to be
        /// handled.
        /// </summary>
        private readonly ConcurrentBag<Packet> receivedUnknownPacketHandlerPackets = new ConcurrentBag<Packet>();

        /// <summary>
        /// Holds all <see cref="Packet"/>s that are handled and are now ready and waiting to be sent to the network.
        /// </summary>
        private readonly ConcurrentQueue<(Packet packet, object senderInstance)> sendPackets =
            new ConcurrentQueue<(Packet packet, object senderInstance)>();

        /// <summary>
        /// Holds all received <see cref="Packet"/>s whose <see cref="Packet.ID"/> is not known.
        /// </summary>
        private readonly ConcurrentQueue<(Packet packet, object senderInstance)> pendingUnknownPackets =
            new ConcurrentQueue<(Packet packet, object senderInstance)>();

        /// <summary>
        /// Maps a <see cref="Packet"/> <see cref="Type"/> to a unique <see cref="ushort"/> ID.
        /// </summary>
        private readonly BiDictionary<Type, ushort> packetTypeToTypeIdMap = new BiDictionary<Type, ushort>();

        /// <summary>
        /// Caches the <see cref="Type"/>s for the <see cref="PacketTypeAttribute"/> and <see cref="ResponsePacketForAttribute"/>
        /// to avoid unnecessary, slow reflection during the addition of <see cref="Packet"/>s from other <see cref="Assembly"/>s.
        /// </summary>
        private readonly Type packetTypeAttribute = typeof(PacketTypeAttribute), responsePacketAttribute = typeof(ResponsePacketForAttribute);

        /// <summary>
        /// The value from which new IDs for packet <see cref="Type"/>s will be calculated dynamically. Starts at 100
        /// as the library already has built-in packets.
        /// </summary>
        private int currentTypeByteIndex = 100;

        /// <summary>
        /// Maps a <see cref="RequestPacket"/> <see cref="Type"/> to the <see cref="Type"/> of the <see cref="ResponsePacket"/>
        /// that handles it.
        /// </summary>
        private readonly Dictionary<Type, Type> requestPacketToResponsePacketMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Maps <see cref="Packet"/> <see cref="Type"/>s to the <see cref="PacketReceivedHandler{P}"/> that should be used for
        /// that <see cref="Packet"/>.
        /// </summary>
        private readonly PacketHandlerMap packetHandlerMap = new PacketHandlerMap();

        #endregion Packet Variables

        #region Thread Variables

        /// <summary>
        /// Reads packets from the network and places them into the <see cref="receivedPackets"/> and
        /// <see cref="receivedUnknownPacketHandlerPackets"/> queues.
        /// </summary>
        private Thread readStreamThread;

        /// <summary>
        /// An event set whenever a packet is received from the network. Used to save CPU time when waiting for a packet to be
        /// received.
        /// </summary>
        private readonly AutoResetEvent packetAvailableEvent = new AutoResetEvent(false);

        /// <summary>
        /// Handles received packets by invoked their respective <see cref="PacketReceivedHandler{P}"/>.
        /// </summary>
        private Thread invokePacketThread;

        /// <summary>
        /// Sends pending packets to the network from the <see cref="sendPackets"/> queue.
        /// </summary>
        private Thread writeStreamThread;

        /// <summary>
        /// An event set whenever a packet is available to be sent to the network. Used to save CPU time when waiting to
        /// send a packet.
        /// </summary>
        private readonly AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);

        #endregion Thread Variables

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        internal Connection()
        {
            //Set the hashCode of this instance.
            hashCode = this.GenerateUniqueHashCode();
            AddExternalPackets(Assembly.GetAssembly(typeof(Packet)));
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The timeout value in milliseconds. If the connection does not receive any packet within the specified timeout,
        /// the connection will timeout and shutdown.
        /// </summary>
        public int ReceiveTimeout { get; protected set; } = 2500;

        /// <summary>
        /// Gets or sets the performance of this <see cref="Connection"/>. The higher the sleep intervals (the lower the performance),
        /// the slower this <see cref="Connection"/> will handle incoming, pending handling, and outgoing <see cref="Packet"/>s.
        /// </summary>
        public Performance Performance { get; set; } = Performance.Default;

        /// <summary>
        /// Casts the <see cref="Performance"/> value to an <see cref="int"/>.
        /// </summary>
        public int IntPerformance { get { return (int)Performance; } }

        /// <summary>
        /// Whether this <see cref="Connection"/> is alive and able to communicate with the <see cref="Connection"/> at
        /// the <see cref="RemoteIPEndPoint"/>.
        /// </summary>
        public bool IsAlive { get { return readStreamThread.IsAlive && invokePacketThread.IsAlive && writeStreamThread.IsAlive; } }

        #region Socket Properties

        /// <summary>
        /// The local <see cref="IPEndPoint"/> for this <see cref="Connection"/> instance.
        /// </summary>
        public abstract IPEndPoint LocalIPEndPoint { get; }

        /// <summary>
        /// The remote <see cref="IPEndPoint"/> that this <see cref="Connection"/> instance communicates with.
        /// </summary>
        public abstract IPEndPoint RemoteIPEndPoint { get; }

        /// <summary>
        /// Whether this <see cref="Connection"/> can operate in dual IPv4 / IPv6 mode.
        /// </summary>
        public abstract bool DualMode { get; set; }

        /// <summary>
        /// Whether sending a packet flushes underlying <see cref="NetworkStream"/>.
        /// </summary>
        /// <remarks>
        /// This value is only used in a <see cref="TcpConnection"/> instance, which uses a <see cref="NetworkStream"/>
        /// to send and receive data. A <see cref="UdpConnection"/> is unaffected by this value.
        /// </remarks>
        public bool ForceFlush { get; set; } = true;

        /// <summary>
        /// Whether this <see cref="Connection"/> is allowed to fragment frames that are too large to send in one go.
        /// </summary>
        public abstract bool Fragment { get; set; }

        /// <summary>
        /// The hop limit for packets sent by this <see cref="Connection"/>. Comparable to IPv4s TTL (Time To Live).
        /// </summary>
        public abstract int HopLimit { get; set; }

        /// <summary>
        /// Whether the packet should be sent directly to its destination or allowed to be routed through multiple destinations
        /// first.
        /// </summary>
        public abstract bool IsRoutingEnabled { get; set; }

        /// <summary>
        /// Whether the packet should be send with or without any delay. If disabled, no data will be buffered at all and
        /// sent immediately to it's destination. There is no guarantee that the network performance will be increased.
        /// </summary>
        public abstract bool NoDelay { get; set; }

        /// <summary>
        /// The 'Time To Live' for this <see cref="Connection"/>.
        /// </summary>
        public abstract short TTL { get; set; }

        /// <summary>
        /// Whether this <see cref="Connection"/> should use a loopback address and bypass hardware.
        /// </summary>
        public abstract bool UseLoopback { get; set; }

        #endregion Socket Properties

        /// <summary>
        /// Whether this <see cref="Connection"/> should send a keep alive packet to the <see cref="RemoteIPEndPoint"/> at
        /// specific intervals, to ensure whether there is a remote <see cref="Connection"/> or not. If set to <c>false</c>
        /// <see cref="RTT"/> and <see cref="Ping"/> won't be refreshed automatically.
        /// </summary>
        public bool KeepAlive
        {
            get
            {
                return keepAlive;
            }
            set
            {
                keepAlive = value;
                ConfigPing(keepAlive);
            }
        }

        /// <summary>
        /// The Round Trip Time for a packet.
        /// </summary>
        public virtual long RTT { get; protected set; }

        /// <summary>
        /// The ping to the <see cref="RemoteIPEndPoint"/>.
        /// </summary>
        public virtual long Ping { get; protected set; }

        /// <summary>
        /// The amount of <see cref="Packets"/> that are pending handling that this <see cref="Connection"/> will buffer.
        /// If we receive a packet which has no handler, it will be buffered for future handler registrations (via
        /// <see cref="RegisterStaticPacketHandler{P}(PacketReceivedHandler{P})"/>,
        /// <see cref="RegisterPacketHandler{P}(PacketReceivedHandler{P}, object)"/>, and <see cref="RegisterRawDataHandler"/>
        /// or their overloads). This value indicates the maximum amount of <see cref="Packet"/>s that will be buffered
        /// before any are dropped.
        /// </summary>
        /// <value>The packet buffer.</value>
        public int PacketBuffer { get; set; } = 1000;

        /// <summary>
        /// Allows the usage of a custom <see cref="IPacketConverter"/> implementation for serialisation and deserialisation.
        /// However, the internal structure of the packet should stay the same:
        ///     Packet Type     : 2  bytes (ushort)
        ///     Packet Length   : 4  bytes (int)
        ///     Packet Data     : xx bytes (actual serialised packet data)
        /// </summary>
        /// <remarks>
        /// Since the default <see cref="PacketConverter"/> uses reflection (with type property caching) for serialisation
        /// and deserialisation. This allows the best performance over the widest range of packets. Should you want to
        /// handle a specific set of packets, a custom <see cref="IPacketConverter"/> can allow more throughput (no slowdowns
        /// due to relatively slow reflection).
        /// </remarks>
        public virtual IPacketConverter PacketConverter
        {
            get { return packetConverter; }
            set { packetConverter = value; }
        }

        /// <summary>
        /// All the currently registered <see cref="PacketReceivedHandler{T}"/>s.
        /// </summary>
        [Obsolete("Use 'BackupPacketHandler' instead")]
        internal PacketHandlerMap PacketHandlerMapper { get { return packetHandlerMap; } }

        #endregion Properties

        #region Methods

        #region Overrides of Object

        /// <summary>
        /// Returns The unique hashcode of this <see cref="Connection"/> instance.
        /// </summary>
        /// <returns>A unique hashcode for this <see cref="Connection"/> instance.</returns>
        public override int GetHashCode() => hashCode;

        /// <summary>
        /// Gets the <see cref="string"/> representation of this <see cref="Connection"/> instance.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of this <see cref="Connection"/> instance.</returns>
        public override string ToString() => $"Local: {LocalIPEndPoint?.ToString() ?? "Null"} Remote: {RemoteIPEndPoint?.ToString() ?? "Null"}";

        #endregion Overrides of Object

        #region Implementation of IPacketHandler

        /// <inheritdoc />
        public void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            packetHandlerMap.RegisterStaticPacketHandler(handler);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <summary>
        /// Registers the given <see cref="Delegate"/> method for all received <see cref="Packet"/>s of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> the given <see cref="Delegate"/> method should be invoked for.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="Delegate"/> method to be invoked for each received <see cref="Packet"/> of the given type.
        /// </param>
        internal void RegisterStaticPacketHandler<P>(Delegate handler) where P : Packet
        {
            packetHandlerMap.RegisterStaticPacketHandler<P>(handler);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <inheritdoc />
        public void RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            packetHandlerMap.RegisterPacketHandler<P>(handler, obj);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <summary>
        /// Registers the given <see cref="Delegate"/> method for all received <see cref="Packet"/>s of the given type
        /// that are received on the given <see cref="object"/> instance.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> the given <see cref="Delegate"/> method should be invoked for.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="Delegate"/> method to be invoked for each received <see cref="Packet"/> of the given type.
        /// </param>
        /// <param name="obj">
        /// The <see cref="object"/> that should handle the received <see cref="Packet"/>s, i.e. the <see cref="object"/> on which
        /// the given <see cref="Delegate"/> method is called.
        /// </param>
        internal void RegisterPacketHandler<P>(Delegate handler, object obj) where P : Packet
        {
            packetHandlerMap.RegisterPacketHandler<P>(handler, obj);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{T}"/> for all <see cref="RawData"/> packets with the
        /// given <see cref="string"/> key.
        /// </summary>
        /// <param name="key">
        /// The <see cref="string"/> key whose <see cref="Packet"/> should be handled by the given
        /// <see cref="PacketReceivedHandler{T}"/>.
        /// </param>
        /// <param name="handler">
        /// The <see cref="PacketReceivedHandler{T}"/> delegate to be invoked for each received <see cref="RawData"/>
        /// packet with the given key.
        /// </param>
        public void RegisterRawDataHandler(string key, PacketReceivedHandler<RawData> handler)
        {
            packetHandlerMap.RegisterStaticRawDataHandler(key, handler);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <inheritdoc />
        public void UnRegisterStaticPacketHandler<T>() where T : Packet
        {
            packetHandlerMap.DeregisterStaticPacketHandler<T>();
        }

        /// <inheritdoc />
        public void UnRegisterPacketHandler<T>(object obj) where T : Packet
        {
            packetHandlerMap.DeregisterPacketHandler<T>(obj);
        }

        /// <summary>
        /// Deregisters the given <see cref="PacketReceivedHandler{T}"/> for all <see cref="RawData"/> packets with the
        /// given <see cref="string"/> key.
        /// </summary>
        /// <param name="key">
        /// The <see cref="string"/> key whose <see cref="PacketReceivedHandler{T}"/> delegate method to deregister.
        /// </param>
        public void UnRegisterRawDataHandler(string key)
        {
            packetHandlerMap.DeregisterStaticRawDataHandler(key);
        }

        #endregion Implementation of IPacketHandler

        /// <summary>
        /// Partial method for initialising addon variables.
        /// </summary>
        partial void InitialiseAddons();

        /// <summary>
        /// Initialises this <see cref="Connection"/> instance, setting up all required variables.
        /// </summary>
        internal void Initialise()
        {
            InitialiseAddons();

            readStreamThread = new Thread(ReadWork)
            {
                Priority = ThreadPriority.Normal,
                Name = $"Read Thread {LocalIPEndPoint.AddressFamily.ToString()}",
                IsBackground = true
            };

            writeStreamThread = new Thread(WriteWork)
            {
                Priority = ThreadPriority.Normal,
                Name = $"Write Thread {LocalIPEndPoint.AddressFamily.ToString()}",
                IsBackground = true
            };

            invokePacketThread = new Thread(InvokeWork)
            {
                Priority = ThreadPriority.Normal,
                Name = $"Invoke Thread {LocalIPEndPoint.AddressFamily.ToString()}",
                IsBackground = true
            };

            readStreamThread.Start();
            writeStreamThread.Start();
            invokePacketThread.Start();
        }

        /// <summary>
        /// Registers all <see cref="Packet"/> inheritors in the given <see cref="Assembly"/> with this <see cref="Connection"/>.
        /// Should this method be called manually, it must be called on both the server and client so that all <see cref="Packet"/>s
        /// in use are known to all parties (avoids incompatible states; exception thrown otherwise).
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to search in for inheritors of <see cref="Packet"/>.</param>
        /// <remarks>
        /// All packets in the network lib are included by default. A manual call is not essential, even if the used packets
        /// are not included, as the library will attempt to synchronise known <see cref="Packet"/>s between the server and client
        /// automatically.
        /// </remarks>
        internal void AddExternalPackets(Assembly assembly)
        {
            Type packetType = typeof(Packet);

            foreach (Type type in assembly.GetTypes())
            {
                //Skip any types that don't inherit from 'Packet'
                if (!type.IsSubclassOf(packetType))
                    continue;

                //This assembly has already been added
                if (packetTypeToTypeIdMap.ContainsKey(type))
                    return;

                Attribute packetIdAttribute = type.GetCustomAttribute(packetTypeAttribute);
                Attribute requestPacketAttribute = type.GetCustomAttribute(responsePacketAttribute);

                //Generate ID for type, using dynamic ID if one is not declared explicitly, and add it to the packet type map
                ushort packetId = ((PacketTypeAttribute)packetIdAttribute)?.Id ?? (ushort)Interlocked.Increment(ref currentTypeByteIndex);
                packetTypeToTypeIdMap[type] = packetId;

                //Check to see if a response packet is being handled, and register it if it hasn't already been registered
                Type requestPacketHandled = ((ResponsePacketForAttribute)requestPacketAttribute).RequestType;

                if (requestPacketHandled == null)
                    continue;

                if (!requestPacketToResponsePacketMap.ContainsKey(requestPacketHandled))
                    requestPacketToResponsePacketMap[requestPacketHandled] = type;
            }
        }

        #region Packet Handler Map Manipulation

        /// <summary>
        /// Returns the current <see cref="PacketHandlerMap"/> instance, so that
        /// the types of packets handled can be read.
        /// </summary>
        /// <returns>
        /// The current <see cref="PacketHandlerMap"/> instance used by this
        /// connection.
        /// </returns>
        public PacketHandlerMap BackupPacketHandler() => packetHandlerMap;

        /// <summary>
        /// Invoked whenever the <see cref="packetHandlerMap"/> gets refreshed.
        /// </summary>
        public virtual void PacketHandlerMapRefreshed() { }

        /// <summary>
        /// Invoked whenever the <see cref="packetHandlerMap"/> gets refreshed.
        /// </summary>
        [Obsolete("Use 'PacketHandlerMapRefreshed' instead.")]
        public void ObjectMapRefreshed() => PacketHandlerMapRefreshed();

        /// <summary>
        /// Restores the <see cref="packetHandlerMap"/> to the given state.
        /// </summary>
        /// <param name="newState">The state to which to restore the internal <see cref="packetHandlerMap"/>.</param>
        internal void RestorePacketHandler(PacketHandlerMap newState)
        {
            packetHandlerMap.Restore(newState);
            PacketHandlerMapRefreshed();
        }

        #endregion Packet Handler Map Manipulation

        /// <summary>
        /// Configures the <see cref="nextPingStopWatch"/> timer.
        /// </summary>
        /// <param name="enable">
        /// Whether the <see cref="nextPingStopWatch"/> should be enabled on reconfiguring.
        /// </param>
        private void ConfigPing(bool enable)
        {
#if !DEBUG
            if (enable)
                nextPingStopWatch.Restart();
            else
                nextPingStopWatch.Reset();
#endif
        }

        #region Sending Packets

        /// <summary>
        /// Serialises the given <see cref="Packet"/> using the current <see cref="PacketConverter"/> and queues it to be sent to the network.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be sent across the network.</param>
        /// <param name="senderInstance">The <see cref="object"/> instance which sent the packet.</param>
        internal void SendInternal(Packet packet, object senderInstance)
        {
            //Cache the packet type
            Type packetType = packet.GetType();

            bool packetTypeIsNotKnown = !packetTypeToTypeIdMap.ContainsKey(packetType) ||
                                 pendingUnknownPackets.Any(p => p.packet.GetType().Assembly.Equals(packetType.Assembly));

            //Synchronise knowledge of packet type across connections
            if (packetTypeIsNotKnown)
            {
                AddExternalPackets(packetType.Assembly);
                pendingUnknownPackets.Enqueue((packet, senderInstance));

                Send(new AddPacketTypeRequest(packetType.Assembly.FullName));

                //We delay sending until both connections are aware of the packet's parent assembly
                return;
            }

            //Packet type is known by both connections, so we can send the packet
            sendPackets.Enqueue((packet, senderInstance));
            dataAvailableEvent.Set();
        }

        /// <summary>
        /// Serialises the given <see cref="Packet"/> using the current <see cref="PacketConverter"/> and sends it to the network.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be sent across the network.</param>
        /// <param name="senderInstance">The <see cref="object"/> instance which sent the packet.</param>
        public void Send(Packet packet, object senderInstance) => SendInternal(packet, senderInstance);

        /// <summary>
        /// Serialises the given <see cref="Packet"/> using the current <see cref="PacketConverter"/>, and sends it to the network.
        /// No response is possible as a sender instance is not provided. This method is suitable for static classes and basic packets
        /// with no inheritance.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to serialise and send.</param>
        public void Send(Packet packet) => Send(packet, null);

        /// <summary>
        /// Sends the given <see cref="Packet"/> and asynchronously awaits the <see cref="ResponsePacket"/>.
        /// </summary>
        /// <typeparam name="R">The <see cref="ResponsePacket"/> type to await.</typeparam>
        /// <param name="packet">The <see cref="Packet"/> to send.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the promise of a <see cref="ResponsePacket"/> of
        /// the given type upon completion.
        /// </returns>
        public async Task<R> SendAsync<R>(Packet packet) where R : ResponsePacket => await new ChickenReceiver().Send<R>(packet, this);

        /// <summary>
        /// Sends a <see cref="PingRequest"/>, if a <see cref="PingResponse"/> is not currently being awaited.
        /// </summary>
        public void SendPing()
        {
            if (currentPingStopWatch.IsRunning)
                return;

            nextPingStopWatch.Reset();
            currentPingStopWatch.Restart();

            Send(new PingRequest());
        }

        /// <summary>
        /// Sends the given <see cref="RawData"/> packet to the network.
        /// </summary>
        /// <param name="rawData">The <see cref="RawData"/> packet to send to the network.</param>
        public void SendRawData(RawData rawData) => Send(rawData);

        /// <summary>
        /// Sends the given raw, serialised primitive to the network.
        /// </summary>
        /// <param name="key">
        /// The <see cref="string"/> key which identifies the raw data <see cref="PacketReceivedHandler{P}"/> to use for the data.
        /// </param>
        /// <param name="data">The serialised raw primitive, as a <see cref="byte"/> array.</param>
        public void SendRawData(string key, byte[] data)
        {
            if (data == null)
            {
                Logger.Log("Can't send a null reference data byte array", new ArgumentException(nameof(data)));
                return;
            }

            Send(new RawData(key, data));
        }

        #endregion Sending Packets

        #region Threads

        /// <summary>
        /// Reads bytes from the network.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        protected abstract byte[] ReadBytes(int amount);

        /// <summary>
        /// Writes bytes to the network.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        protected abstract void WriteBytes(byte[] bytes);

        /// <summary>
        /// Reads <see cref="Packet"/> objects from the network and queues them in the <see cref="receivedPackets"/> queue.
        /// </summary>
        private void ReadWork()
        {
            try
            {
                while (true)
                {
                    ushort packetType = BitConverter.ToUInt16(ReadBytes(2), 0);
                    int packetLength = BitConverter.ToInt32(ReadBytes(4), 0);

                    byte[] packetData = ReadBytes(packetLength);

                    if (!packetTypeToTypeIdMap.ContainsValue(packetType))
                    {
                        //Theoretically it is not possible that we receive a packet which is not known, since all the packets
                        //need to pass a certification. But if the UDP connection sends something and the AddPacketTypeRequest
                        //get lost this case is going to happen.
                        HandleUnknownPacket();
                        continue;
                    }

                    Packet receivedPacket = packetConverter.DeserialisePacket(packetTypeToTypeIdMap[packetType], packetData);
                    receivedPacket.Size = packetLength;

                    receivedPackets.Enqueue(receivedPacket);

                    packetAvailableEvent.Set();

                    Logger.LogInComingPacket(packetData, receivedPacket);
                }
            }
            catch (ThreadAbortException) { return; }
            catch (Exception exception)
            {
                Logger.Log("Reading packet from stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.ReadPacketThreadException);
        }

        /// <summary>
        /// Invokes the relevant <see cref="PacketReceivedHandler{P}"/> for each packet in the <see cref="receivedPackets"/> queue.
        /// </summary>
        private void InvokeWork()
        {
            try
            {
                while (true)
                {
                    //Wait till a packet is available.
                    packetAvailableEvent.WaitOne();

                    while (receivedPackets.Count > 0)
                    {
                        if (!receivedPackets.TryDequeue(out Packet packetToHandle))
                            continue;

                        packetToHandle.BeforeReceive();
                        HandleDefaultPackets(packetToHandle);
                    }
                }
            }
            catch (ThreadAbortException) { return; }
            catch (Exception exception)
            {
                Logger.Log("Handling received packet", exception, LogLevel.Warning);
            }

            CloseHandler(CloseReason.InvokePacketThreadException);
        }

        /// <summary>
        /// Writes all queued <see cref="Packet"/> objects in the <see cref="sendPackets"/> queue to the network.
        /// </summary>
        private void WriteSubWork()
        {
            while (sendPackets.Count > 0)
            {
                if (!sendPackets.TryDequeue(out (Packet packet, object listener) packetWithObject))
                    continue;

                Packet packet = packetWithObject.packet;
                Type packetType = packet.GetType();

                //Insert the ID into the packet if it is an request packet.
                if (packet.GetType().IsSubclassOf(typeof(RequestPacket)) && packetWithObject.listener != null)
                    packet.ID = packetHandlerMap[requestPacketToResponsePacketMap[packet.GetType()], packetWithObject.listener];

                //Prepare some data in the packet.
                packet.BeforeSend();

                /*              Packet structure:
                                1. [16 bits] packet type
                                2. [32 bits] packet length
                                3. [xx bits] packet data                 */

                byte[] packetHeader = new byte[6];
                byte[] packetData = packetConverter.SerialisePacket(packet);
                byte[] fullPacket = new byte[6 + packetData.Length];

                ushort packetId = packetTypeToTypeIdMap[packetType];
                int packetLength = fullPacket.Length;

                //Write out the packet header
                packetHeader[0] = (byte)(packetId);
                packetHeader[1] = (byte)(packetId >> 8);
                packetHeader[2] = (byte)packetLength;
                packetHeader[3] = (byte)(packetLength >> 8);
                packetHeader[4] = (byte)(packetLength >> 16);
                packetHeader[5] = (byte)(packetLength >> 24);

                Buffer.BlockCopy(packetHeader, 0, fullPacket, 0, 6);
                Buffer.BlockCopy(packetData, 0, fullPacket, 6, packetLength - 6);

                WriteBytes(fullPacket);

                Logger.LogOutgoingPacket(packetData, packet);
            }
        }

        /// <summary>
        /// Writes any packets queued up in the <see cref="sendPackets"/> queue to the network.
        /// </summary>
        private void WriteWork()
        {
            try
            {
                while (true)
                {
                    //Wait for data to become available to send
                    dataAvailableEvent.WaitOne();

                    WriteSubWork();

                    //The remote connection could have died after sending packets
                    if (KeepAlive && nextPingStopWatch.ElapsedMilliseconds >= PingInterval)
                    {
                        SendPing();
                    }
                    else if (currentPingStopWatch.ElapsedMilliseconds >= ReceiveTimeout && currentPingStopWatch.ElapsedMilliseconds != 0)
                    {
                        ConfigPing(KeepAlive);
                        currentPingStopWatch.Reset();
                        CloseHandler(CloseReason.Timeout);
                    }
                }
            }
            catch (ThreadAbortException) { return; }
            catch (Exception exception)
            {
                Logger.Log("Write packet to stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.WritePacketThreadException);
        }

        #endregion Threads

        #region Handling Received Packets

        /// <summary>
        /// Handles all default <see cref="Packet"/>s that are in the library.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be handled.</param>
        private void HandleDefaultPackets(Packet packet)
        {
            Type packetType = packet.GetType();

            if (packetType == typeof(PingRequest))
            {
                Send(new PingResponse());
            }
            else if (packetType == typeof(PingResponse))
            {
                long elapsedTime = currentPingStopWatch.ElapsedMilliseconds;
                currentPingStopWatch.Reset();
                nextPingStopWatch.Restart();

                Ping = elapsedTime / 2;
                RTT = elapsedTime;
            }
            else if (packetType == typeof(CloseRequest))
            {
                CloseRequest closeRequest = (CloseRequest)packet;
                ExternalClose(closeRequest.CloseReason);
            }
            else if (packetType == typeof(EstablishUdpRequest))
            {
                EstablishUdpRequest establishUdpRequest = (EstablishUdpRequest)packet;
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, GetFreePort());

                Send(new EstablishUdpResponse(udpEndPoint.Port, establishUdpRequest));

                UdpConnection udpConnection =
                    CreateUdpConnection(udpEndPoint, new IPEndPoint(RemoteIPEndPoint.Address, establishUdpRequest.UdpPort), true);

                pendingUDPConnections.Enqueue(udpConnection);
                OnConnectionEstablished((TcpConnection)this, udpConnection);
            }
            else if (packetType == typeof(EstablishUdpResponseACK))
            {
                UdpConnection udpConnection;

                while (!pendingUDPConnections.TryDequeue(out udpConnection))
                    Thread.Sleep(IntPerformance);

                udpConnection.AcknowledgePending = false;
            }
            else if (packetType == typeof(AddPacketTypeRequest))
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .SingleOrDefault(a => a.FullName == ((AddPacketTypeRequest)packet).AssemblyName);

                if (assembly == null)
                {
                    CloseHandler(CloseReason.AssemblyDoesNotExist);
                }
                else
                {
                    AddExternalPackets(assembly);
                }

                Send(new AddPacketTypeResponse(packetTypeToTypeIdMap.Values.ToList(), (AddPacketTypeRequest)packet));
            }
            else if (packetType == typeof(AddPacketTypeResponse))
            {
                List<(Packet packet, object instance)> internalSendQueue = new List<(Packet packet, object instance)>();
                AddPacketTypeResponse addPacketTypeResponse = (AddPacketTypeResponse)packet;

                //Remove all packets of this type and send them :)
                while (true)
                {
                    (Packet packet, object listener) toSend;
                    while (!pendingUnknownPackets.TryPeek(out toSend) && pendingUnknownPackets.Count > 0)
                        Thread.Sleep(IntPerformance); //Wait till we got a result.

                    //If the other connection contains that packet, send it.
                    if (toSend.packet != null && addPacketTypeResponse.LocalDict.Contains(packetTypeToTypeIdMap[toSend.packet.GetType()]))
                    {
                        while (!pendingUnknownPackets.TryDequeue(out toSend))
                            Thread.Sleep(IntPerformance); //Wait till we got a result.

                        internalSendQueue.Add((toSend.packet, toSend.listener));
                        continue;
                    }

                    //Now the pendingUnknownPackets queue doesn't contain those elements any more.
                    internalSendQueue.ForEach(i => Send(i.packet, i.instance));
                    break;
                }
            }
            //Receiving raw data from the connection.
            else if (packetType == typeof(RawData))
            {
                RawData rawData = (RawData)packet;

                if (packetHandlerMap[rawData.Key] == null)
                {
                    Logger.Log($"RawData packet has no listener. Key: {rawData.Key}", LogLevel.Warning);
                }
                else
                {
                    packetHandlerMap[rawData.Key].DynamicInvoke(packet, this);
                }
            }
            else
            {
                try
                {
                    if (packet.GetType().IsSubclassOf(typeof(ResponsePacket)) && packetHandlerMap[packet.ID] != null)
                    {
                        packetHandlerMap[packet.ID].DynamicInvoke(packet, this);
                    }
                    else if (packetHandlerMap[packet.GetType()] != null)
                    {
                        packetHandlerMap[packet.GetType()].DynamicInvoke(packet, this);
                    }
                    else
                    {
                        PacketWithoutHandlerReceived(packet);
                    }
                }
                catch (Exception exception)
                {
                    Logger.Log("Provided delegate contains a bug. Packet invocation thread crashed.", exception, LogLevel.Exception);
                }
            }
        }

        /// <summary>
        /// Invoked for any received <see cref="Packet"/>s that don't have a registered <see cref="PacketReceivedHandler{P}"/>.
        /// </summary>
        /// <param name="packet">The received <see cref="Packet"/> without a handler.</param>
        protected virtual void PacketWithoutHandlerReceived(Packet packet)
        {
            Logger.Log($"Packet with no handler received: {packet.GetType().Name}.", LogLevel.Warning);

            if (receivedUnknownPacketHandlerPackets.Count < PacketBuffer)
            {
                receivedUnknownPacketHandlerPackets.Add(packet);
            }
            else
            {
                Logger.Log($"PacketBuffer exceeded the limit of {PacketBuffer}. Received packet {packet.GetType().Name} dropped.",
                    LogLevel.Error);
            }
        }

        /// <summary>
        /// Handles the unknown packet.
        /// </summary>
        protected abstract void HandleUnknownPacket();

        /// <summary>
        /// Whenever a <see cref="Packet"/> is received without a <see cref="PacketReceivedHandler{T}"/> already registered for
        /// it, it is placed in a timeout in the sad, lonely corner that is <see cref="receivedUnknownPacketHandlerPackets"/>.
        /// Whenever a <see cref="PacketReceivedHandler{P}"/> is registered via the 'Connection.RegisterXXX' methods, we need to
        /// search for any lonely packets that can now be handled.
        /// </summary>
        private void SearchAndInvokeUnknownHandlerPackets(Delegate del)
        {
            //Retrieve the packet type for the given handler.
            Type delegateForPacketType = del.GetType().GenericTypeArguments.FirstOrDefault();

            if (receivedUnknownPacketHandlerPackets.Any(p => p.GetType() == delegateForPacketType))
            {
                IEnumerable<Packet> forwardToDelegatePackets =
                    receivedUnknownPacketHandlerPackets.Where(p => p.GetType() == delegateForPacketType);

                foreach (Packet currentForwardPacket in forwardToDelegatePackets)
                {
                    Logger.Log($"Buffered packet {currentForwardPacket.GetType().Name} now has a handler => Handling");
                    receivedUnknownPacketHandlerPackets.Remove(currentForwardPacket);
                    HandleDefaultPackets(currentForwardPacket);
                }
            }
        }

        #endregion Handling Received Packets

        #region Closing The Connection

        /// <summary>
        /// Closes the socket and frees all associated resources.
        /// </summary>
        protected abstract void CloseSocket();

        /// <summary>
        /// Handles a <see cref="Connection"/> closure, with the given <see cref="CloseReason"/>.
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> closing.</param>
        protected abstract void CloseHandler(CloseReason closeReason);

        /// <summary>
        /// Handles a case where the remote <see cref="Connection"/> caused a closure for the given <see cref="CloseReason"/>.
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> closing.</param>
        internal void ExternalClose(CloseReason closeReason)
        {
            writeStreamThread.AbortSave();
            readStreamThread.AbortSave();
            OnConnectionClosed(closeReason, this);
            invokePacketThread.AbortSave();
            CloseSocket();
        }

        /// <summary>
        /// Closes this <see cref="Connection"/>, sends a <see cref="CloseRequest"/> to the remote <see cref="Connection"/>,
        /// and writes all remaining queued <see cref="Packet"/>s to the network (they are received before the <see cref="CloseRequest"/>
        /// will be handled).
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> closing.</param>
        /// <param name="callCloseEvent">If this <see cref="Connection"/> instance should call its <see cref="Connection"/> event.</param>
        public void Close(CloseReason closeReason, bool callCloseEvent = false)
        {
            //Check if this connection is already dead. If so, there is no need to
            //handle an exception or anything else.
            if (!IsAlive)
                return;

            try
            {
                Send(new CloseRequest(closeReason), true);
                WriteSubWork(); //Force to write the remaining packets.
            }
            catch (Exception exception)
            {
                Logger.Log($"Couldn't send a close-message '{closeReason.ToString()}' to the endpoint.", exception, LogLevel.Warning);
            }

            if (callCloseEvent)
                OnConnectionClosed(closeReason, this);

            writeStreamThread.AbortSave();
            readStreamThread.AbortSave();
            invokePacketThread.AbortSave();
            CloseSocket();
        }

        #endregion Closing The Connection

        /// <summary>
        /// Creates and returns a new <see cref="UdpConnection"/>, with the given local endpoint, remote endpoint, and write lock state.
        /// </summary>
        /// <param name="localEndPoint">The local <see cref="IPEndPoint"/> that the <see cref="UdpConnection"/> binds to.</param>
        /// <param name="remoteEndPoint">The remote <see cref="IPEndPoint"/> that the <see cref="UdpConnection"/> talks to.</param>
        /// <param name="writeLock">Whether the <see cref="UdpConnection"/> has a write lock.</param>
        /// <returns>The instantiated <see cref="UdpConnection"/>.</returns>
        protected virtual UdpConnection CreateUdpConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, bool writeLock) =>
            new UdpConnection(new UdpClient(localEndPoint), remoteEndPoint, writeLock);

        /// <summary>
        /// Gets a free port that is currently not in use and returns it.
        /// </summary>
        /// <returns>The port found.</returns>
        protected int GetFreePort()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Unlocks the connection and allows for data to be sent and received.
        /// </summary>
        [Obsolete("Unlocking a connection isn't required anymore.")]
        public void UnlockRemoteConnection() => Logger.Log($"UnlockRemoteConnection will be removed in a future release.", LogLevel.Warning);

        #endregion Methods

        #region Events

        /// <summary>
        /// Invokes the <see cref="ConnectionEstablished"/> event with the given <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="newTcpConnection">The new <see cref="TcpConnection"/> that was established.</param>
        /// <param name="newUdpConnection">The new <see cref="UdpConnection"/> that was established.</param>
        protected void OnConnectionEstablished(TcpConnection newTcpConnection, UdpConnection newUdpConnection)
        {
            ConnectionEstablished?.Invoke(newTcpConnection, newUdpConnection);
        }

        /// <summary>
        /// Adds or remove an action which will be invoked if the connection
        /// created a new UDP connection. The delivered tcpConnection represents the tcp connection
        /// which was in charge of the new establishment.
        /// </summary>
        public event Action<TcpConnection, UdpConnection> ConnectionEstablished;

        /// <summary>
        /// Invokes the <see cref="ConnectionClosed"/> event with the given <see cref="CloseReason"/> and closed <see cref="Connection"/>.
        /// </summary>
        /// <param name="closureReason">The reason that the given <see cref="Connection"/> was closed.</param>
        /// <param name="closedConnection">The <see cref="Connection"/> that was closed.</param>
        protected void OnConnectionClosed(CloseReason closureReason, Connection closedConnection)
        {
            ConnectionClosed?.Invoke(closureReason, closedConnection);
        }

        /// <summary>
        /// Adds or removes an action which will be invoked if the network dies.
        /// </summary>
        public event Action<CloseReason, Connection> ConnectionClosed;

        #endregion Events
    }
}