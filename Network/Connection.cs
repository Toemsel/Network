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
        protected const int PING_INTERVALL = 10000;

        /// <summary>
        /// A fixed hashcode that persists with the <see cref="Connection"/> instance for its entire lifetime.
        /// </summary>
        private readonly int hashCode;

        // TODO Remove all occurrences of backing fields for events in favor of new, cleaner 'event?.Invoke(args)' syntax

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<CloseReason, Connection> networkConnectionClosed;

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<CloseReason, Connection> connectionClosed;

        /// <summary>
        /// A handler which will be invoked if a new connection is established.
        /// </summary>
        private event Action<TcpConnection, UdpConnection> connectionEstablished;

        /// <summary>
        /// A token source to singal all internal threads to terminate.
        /// </summary>
        private CancellationTokenSource threadCancellationTokenSource = new CancellationTokenSource(Timeout.Infinite);

        #region Ping Variables

        /// <summary>
        /// Whether this instance should send out a keep alive packet at specific intervals, to ensure there is an alive remote connection.
        /// If set to [false] <see cref="RTT"/> and <see cref="Ping"/> wont be enabled/refreshed.
        /// </summary>
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

        #region ResetEvents
        /// <summary>
        /// An event set whenever a packet is received from the network. Used to save CPU time when waiting for a packet to be
        /// received.
        /// </summary>
        private readonly AutoResetEvent packetAvailableEvent = new AutoResetEvent(false);

        /// <summary>
        /// An event set whenever a packet is available to be sent to the network. Used to save CPU time when waiting to
        /// send a packet.
        /// </summary>
        private readonly AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);
        #endregion ResetEvents

        #region Thread Variables
        /// <summary>
        /// Reads packets from the network and places them into the <see cref="receivedPackets"/> and
        /// <see cref="receivedUnknownPacketHandlerPackets"/> queues.
        /// </summary>
        private Thread readStreamThread;

        /// <summary>
        /// Handles received packets by invoked their respective <see cref="PacketReceivedHandler{P}"/>.
        /// </summary>
        private Thread invokePacketThread;

        /// <summary>
        /// Sends pending packets to the network from the <see cref="sendPackets"/> queue.
        /// </summary>
        private Thread writeStreamThread;
        #endregion Thread Variables

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
        /// Holds all received <see cref="Packet"/>s whose <see cref="Packet.ID"/> is not known.
        /// </summary>
        private readonly ConcurrentQueue<Tuple<Packet, object>> pendingUnknownPackets = new ConcurrentQueue<Tuple<Packet, object>>();

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
        private readonly ConcurrentQueue<Tuple<Packet, object>> sendPackets = new ConcurrentQueue<Tuple<Packet, object>>();

        /// <summary>
        /// Maps a <see cref="Packet"/> <see cref="Type"/> to a unique <see cref="ushort"/> ID.
        /// </summary>
        private readonly BiDictionary<Type, ushort> typeByte = new BiDictionary<Type, ushort>();

        /// <summary>
        /// The value from which new IDs for packet <see cref="Type"/>s will be calculated dynamically. Starts at 100
        /// as the library already has built-in packets.
        /// </summary>
        private int currentTypeByteIndex = 100;

        /// <summary>
        /// Maps a <see cref="RequestPacket"/> <see cref="Type"/> to the <see cref="Type"/> of the <see cref="ResponsePacket"/>
        /// that handles it.
        /// </summary>
        private readonly Dictionary<Type, Type> requestResponseMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Maps <see cref="Packet"/> <see cref="Type"/>s to the <see cref="PacketReceivedHandler{P}"/> that should be used for
        /// that <see cref="Packet"/>.
        /// </summary>
        private readonly PacketHandlerMap packetHandlerMap = new PacketHandlerMap();

        #endregion Packet Variables

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

        /// <summary>
        /// Partial initialisation method to be overriden in later class implementations.
        /// </summary>
        partial void InitAddons();

        /// <summary>
        /// Initialises this <see cref="Connection"/> instance, setting up all required variables.
        /// </summary>
        internal void Init()
        {
            InitAddons();

            readStreamThread = new Thread(ReadWork);
            readStreamThread.Priority = ThreadPriority.Normal;
            readStreamThread.Name = $"Read Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            readStreamThread.IsBackground = true;

            writeStreamThread = new Thread(WriteWork);
            writeStreamThread.Priority = ThreadPriority.Normal;
            writeStreamThread.Name = $"Write Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            writeStreamThread.IsBackground = true;

            invokePacketThread = new Thread(InvokeWork);
            invokePacketThread.Priority = ThreadPriority.Normal;
            invokePacketThread.Name = $"Invoke Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            invokePacketThread.IsBackground = true;

            readStreamThread.Start();
            writeStreamThread.Start();
            invokePacketThread.Start();
        }

        #region Properties

#if !DEBUG
        /// <summary>
        /// The timeout value in milliseconds. If the connection does not receive any packet within the specified timeout,
        /// the connection will timeout and shutdown.
        /// </summary>
        public int TIMEOUT { get; set; } = 2500;
#endif

#if DEBUG
        /// <summary>
        /// The timeout value in milliseconds. If the connection does not receive any packet within the specified timeout,
        /// the connection will timeout and shutdown.
        /// </summary>
        public int TIMEOUT { get; set; } = int.MaxValue;
#endif

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
        /// Whether this <see cref="Connection"/> is alive and able to communicate with the <see cref="Connection"/> at
        /// the <see cref="IPRemoteEndPoint"/>.
        /// </summary>
        public bool IsAlive { get { return readStreamThread.IsAlive && writeStreamThread.IsAlive && invokePacketThread.IsAlive && !threadCancellationTokenSource.IsCancellationRequested; } }

#region Socket Properties

        /// <summary>
        /// The local <see cref="IPEndPoint"/> for this <see cref="Connection"/> instance.
        /// </summary>
        public abstract IPEndPoint IPLocalEndPoint { get; }

        /// <summary>
        /// The remote <see cref="IPEndPoint"/> that this <see cref="Connection"/> instance communicates with.
        /// </summary>
        public abstract IPEndPoint IPRemoteEndPoint { get; }

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
        /// Whether this <see cref="Connection"/> should send a keep alive packet to the <see cref="IPRemoteEndPoint"/> at
        /// specific intervals, to ensure whether there is a remote <see cref="Connection"/> or not. If set to <c>false</c>
        /// <see cref="RTT"/> and <see cref="Ping"/> won't be refreshed automatically.
        /// </summary>
        public bool KeepAlive
        {
            get { return keepAlive; }
            set
            {
                keepAlive = value;
                ConfigPing(keepAlive);
            }
        }

        /// <summary>
        /// The Round Trip Time for a packet.
        /// </summary>
        public virtual long RTT { get; protected set; } = 0;

        /// <summary>
        /// The ping to the <see cref="IPRemoteEndPoint"/>.
        /// </summary>
        public virtual long Ping { get; protected set; } = 0;

        /// <summary>
        /// Gets or sets the performance of this <see cref="Connection"/>. The higher the sleep intervals (the lower the performance),
        /// the slower this <see cref="Connection"/> will handle incoming, pending handling, and outgoing <see cref="Packet"/>s.
        /// </summary>
        public Performance Performance { get; set; } = Performance.Default;

        /// <summary>
        /// The value of <see cref="Performance"/>, but simply cast to an <see cref="int"/>.
        /// </summary>
        public int IntPerformance { get { return (int)Performance; } }

        /// <summary>
        /// Allows the usage of a custom <see cref="IPacketConverter"/> implementation for serialisation and deserialisation.
        /// However, the internal structure of the packet should stay the same:
        ///     Packet Type     : 2  bytes (ushort)
        ///     Packet Length   : 4  bytes (int)
        ///     Packet Data     : xx bytes (actual serialised packet data)
        /// </summary>
        /// <remarks>
        /// The default <see cref="PacketConverter"/> uses reflection (with type property caching) for serialisation
        /// and deserialisation. This allows good performance over the widest range of packets. Should you want to
        /// handle only a specific set of packets, a custom <see cref="IPacketConverter"/> can allow more throughput (no slowdowns
        /// due to relatively slow reflection).
        /// </remarks>
        public virtual IPacketConverter PacketConverter
        {
            get { return packetConverter; }
            set { packetConverter = value; }
        }

        /// <summary>
        /// Event signifying that a connection was closed between this <see cref="Connection"/> instance and another <see cref="Connection"/>.
        /// This event is only visible for the network library itself. It garantuees, that the lib itself is capable of receiving connection state changes.
        /// </summary>
        internal event Action<CloseReason, Connection> NetworkConnectionClosed
        {
            add { networkConnectionClosed += value; }
            remove { networkConnectionClosed -= value; }
        }

        /// <summary>
        /// Event signifying that a connection was closed between this <see cref="Connection"/> instance and another <see cref="Connection"/>.
        /// </summary>
        public event Action<CloseReason, Connection> ConnectionClosed
        {
            add { connectionClosed += value; }
            remove { connectionClosed -= value; }
        }

        /// <summary>
        /// Event signifying that this <see cref="Connection"/> instance established a new connection with either a <see cref="TcpConnection"/>
        /// or <see cref="UdpConnection"/> instance.
        /// </summary>
        internal event Action<TcpConnection, UdpConnection> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

#endregion Properties

#region Methods

#region Implementation of IPacketHandler

        /// <inheritdoc />
        public void RegisterStaticPacketHandler<T>(PacketReceivedHandler<T> handler) where T : Packet
        {
            packetHandlerMap.RegisterStaticPacketHandler<T>(handler);
            SearchAndInvokeUnknownHandlerPackets(handler);
        }

        /// <inheritdoc cref="RegisterStaticPacketHandler{T}(PacketReceivedHandler{T})"/>
        internal void RegisterStaticPacketHandler<T>(Delegate del) where T : Packet
        {
            packetHandlerMap.RegisterStaticPacketHandler<T>(del);
            SearchAndInvokeUnknownHandlerPackets(del);
        }

        /// <inheritdoc />
        public void RegisterPacketHandler<T>(PacketReceivedHandler<T> handler, object obj) where T : Packet
        {
            packetHandlerMap.RegisterPacketHandler<T>(handler, obj);
            SearchAndInvokeUnknownHandlerPackets((Delegate)handler);
        }

        /// <inheritdoc cref="RegisterPacketHandler{T}(PacketReceivedHandler{T}, object)"/>
        internal void RegisterPacketHandler<T>(Delegate del, object obj) where T : Packet
        {
            packetHandlerMap.RegisterPacketHandler<T>(del, obj);
            SearchAndInvokeUnknownHandlerPackets(del);
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
            SearchAndInvokeUnknownHandlerPackets((Delegate)handler);
        }

        /// <inheritdoc />
        public void UnRegisterStaticPacketHandler<T>() where T : Packet => packetHandlerMap.UnRegisterStaticPacketHandler<T>();

        /// <inheritdoc />
        public void UnRegisterPacketHandler<T>(object obj) where T : Packet => packetHandlerMap.UnRegisterPacketHandler<T>(obj);

        /// <summary>
        /// Deregisters the given <see cref="PacketReceivedHandler{T}"/> for all <see cref="RawData"/> packets with the
        /// given <see cref="string"/> key.
        /// </summary>
        /// <param name="key">
        /// The <see cref="string"/> key whose <see cref="PacketReceivedHandler{T}"/> delegate method to deregister.
        /// </param>
        public void UnRegisterRawDataHandler(string key) => packetHandlerMap.UnRegisterStaticRawDataHandler(key);

#endregion Implementation of IPacketHandler

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
            assembly.GetTypes().ToList().Where(c => c.IsSubclassOf(typeof(Packet))).ToList().ForEach(p =>
            {
                if (typeByte.ContainsKey(p)) return; //Already in the dictionary.
                ushort packetId = (ushort)Interlocked.Increment(ref currentTypeByteIndex);
                Attribute packetTypeAttribute = p.GetCustomAttribute(typeof(PacketTypeAttribute));
                //Apply the local ID if there exist any.
                if (packetTypeAttribute != null) packetId = ((PacketTypeAttribute)packetTypeAttribute).Id;
                typeByte[p] = packetId;
            });

            assembly.GetTypes().ToList().Where(c => c.GetCustomAttributes(typeof(PacketRequestAttribute)).Count() > 0).ToList().
                ForEach(c =>
                {
                    PacketRequestAttribute requestAttribute = ((PacketRequestAttribute)c.GetCustomAttribute(typeof(PacketRequestAttribute)));
                    // TryAdd will fail if another thread investigates the object.
                    // However, it turned out, that somehow the RequestType has been
                    // already added to the requestResponseMap. Hence, we can ignore the failure.
                    if (!requestResponseMap.ContainsKey(requestAttribute.RequestType))
                        requestResponseMap.Add(requestAttribute.RequestType, c);
                });
        }

#region Packet Handler Manipulation

        /// <summary>
        /// Returns the current <see cref="PacketHandlerMap"/> instance, so that
        /// the types of packets handled can be read.
        /// </summary>
        /// <returns>
        /// The current <see cref="PacketHandlerMap"/> instance used by this
        /// connection.
        /// </returns>
        internal PacketHandlerMap BackupPacketHandler() => packetHandlerMap;

        /// <summary>
        /// Invoked whenever the <see cref="packetHandlerMap"/> gets refreshed.
        /// </summary>
        public virtual void ObjectMapRefreshed() { }

        /// <summary>
        /// Restores the <see cref="packetHandlerMap"/> to the given state.
        /// </summary>
        /// <param name="packetHandlerMap">The state to which to restore the internal <see cref="Connection.packetHandlerMap"/>.</param>
        internal void RestorePacketHandler(PacketHandlerMap packetHandlerMap)
        {
            this.packetHandlerMap.Restore(packetHandlerMap);
            ObjectMapRefreshed();
        }

#endregion Packet Handler Manipulation

        /// <summary>
        /// Configures the <see cref="nextPingStopWatch"/> timer.
        /// </summary>
        /// <param name="enable">
        /// Whether the <see cref="nextPingStopWatch"/> should be enabled on reconfiguring.
        /// </param>
        private void ConfigPing(bool enable)
        {
#if !DEBUG
            if (enable) nextPingStopWatch.Restart();
            else nextPingStopWatch.Reset();
#endif
        }

#region Sending Packets

        /// <summary>
        /// Sends a <see cref="PingRequest"/>, if a <see cref="PingResponse"/> is not currently being awaited.
        /// </summary>
        public void SendPing()
        {
            if (currentPingStopWatch.IsRunning) return;
            nextPingStopWatch.Reset();
            currentPingStopWatch.Restart();
            Send(new PingRequest());
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> and asynchronously awaits the <see cref="ResponsePacket"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="ResponsePacket"/> type to await.</typeparam>
        /// <param name="packet">The <see cref="Packet"/> to send.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the promise of a <see cref="ResponsePacket"/> of
        /// the given type upon completion.
        /// </returns>
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket => await new ChickenReceiver().Send<T>(packet, this);

        /// <summary>
        /// Serialises the given <see cref="Packet"/> using the current <see cref="PacketConverter"/> and queues it to be sent
        /// to the network. No response is possible as a sender instance is not provided. This method is suitable for static
        /// classes and basic packets with no inheritance.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be sent across the network.</param>
        public void Send(Packet packet) => Send(packet, null);

        /// <summary>
        /// Serialises the given <see cref="Packet"/> using the current <see cref="PacketConverter"/> and queues it to be sent
        /// to the network.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be sent across the network.</param>
        /// <param name="instance">The <see cref="object"/> instance which sent the packet.</param>
        public void Send(Packet packet, object instance)
        {
            //Ensure that everyone is aware of that packetType.
            if (!typeByte.ContainsKey(packet.GetType()) || pendingUnknownPackets.Any(p => p.Item1.GetType().Assembly.Equals(packet.GetType().Assembly)))
            {
                AddExternalPackets(packet.GetType().Assembly);
                pendingUnknownPackets.Enqueue(new Tuple<Packet, object>(packet, instance));
                Send(new AddPacketTypeRequest(packet.GetType().Assembly.FullName));
                return; //Wait till we receive green light
            }

            sendPackets.Enqueue(new Tuple<Packet, object>(packet, instance));
            dataAvailableEvent.Set();
        }

#endregion Sending Packets

#region Threads

        /// <summary>
        /// Reads bytes from the network.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read <see cref="byte"/>s</returns>
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

                    if (!typeByte.ContainsValue(packetType))
                    {
                        //Theoretically it is not possible that we receive a packet
                        //which is not known, since all the packets need to pass a certification.
                        //But if the UDP connection sends something and the AddPacketTypeRequest get lost
                        //this case is going to happen.
                        HandleUnknownPacket();
                        continue;
                    }

                    Packet receivedPacket = packetConverter.GetPacket(typeByte[packetType], packetData);
                    receivedPackets.Enqueue(receivedPacket);
                    receivedPacket.Size = packetLength;

                    Logger.LogInComingPacket(packetData, receivedPacket);

                    packetAvailableEvent.Set();
                }
            }
            catch (Exception exception)
            {
                if (!threadCancellationTokenSource.IsCancellationRequested)
                    Logger.Log("Reading packet from stream", exception, LogLevel.Exception);
                else return;
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
                    WaitHandle.WaitAny(new WaitHandle[] { packetAvailableEvent, threadCancellationTokenSource.Token.WaitHandle });

                    // exit the endless loop via an exception if the network threads have been signaled to abort.
                    threadCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    while (receivedPackets.Count > 0)
                    {
                        Packet toDelegate = null;
                        if (!receivedPackets.TryDequeue(out toDelegate))
                            continue;

                        toDelegate.BeforeReceive();
                        HandleDefaultPackets(toDelegate);
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception exception)
            {
                Logger.Log($"Delegating packet to subscribers.", exception, LogLevel.Exception);
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
                Tuple<Packet, object> packetWithObject = null;
                if (!sendPackets.TryDequeue(out packetWithObject))
                    continue;

                Packet packet = packetWithObject.Item1;

                //Insert the ID into the packet if it is an request packet.
                if (packet.GetType().IsSubclassOf(typeof(RequestPacket)) && packetWithObject.Item2 != null)
                    packet.ID = packetHandlerMap[requestResponseMap[packet.GetType()], packetWithObject.Item2];

                //Prepare some data in the packet.
                packet.BeforeSend();

                /*              Packet structure:
                                1. [16bits] packet type
                                2. [32bits] packet length
                                3. [xxbits] packet data                 */

                byte[] packetData = packetConverter.GetBytes(packet);
                byte[] packetLength = BitConverter.GetBytes(packetData.Length);
                byte[] packetByte = new byte[2 + packetLength.Length + packetData.Length];

                packetByte[0] = (byte)(typeByte[packet.GetType()]);
                packetByte[1] = (byte)(typeByte[packet.GetType()] >> 8);
                Array.Copy(packetLength, 0, packetByte, 2, packetLength.Length);
                Array.Copy(packetData, 0, packetByte, 2 + packetLength.Length, packetData.Length);
                WriteBytes(packetByte);

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
                    WaitHandle.WaitAny(new WaitHandle[] { dataAvailableEvent, threadCancellationTokenSource.Token.WaitHandle }, TIMEOUT);

                    // exit the endless loop via an exception if the network threads have been signaled to abort.
                    threadCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    WriteSubWork();

                    //Check if the client is still alive.
                    if (KeepAlive && nextPingStopWatch.ElapsedMilliseconds >= PING_INTERVALL)
                    {
                        SendPing();
                    }
                    else if (currentPingStopWatch.ElapsedMilliseconds >= TIMEOUT &&
                        currentPingStopWatch.ElapsedMilliseconds != 0)
                    {
                        ConfigPing(KeepAlive);
                        currentPingStopWatch.Reset();
                        CloseHandler(CloseReason.Timeout);
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception exception)
            {
                Logger.Log("Write object on stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.WritePacketThreadException);
        }

#endregion Threads

#region Handling Packets

        /// <summary>
        /// Handles all default <see cref="Packet"/>s that are in the library.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> to be handled.</param>
        private void HandleDefaultPackets(Packet packet)
        {
            if (packet.GetType().Equals(typeof(PingRequest)))
            {
                Send(new PingResponse());
                return;
            }
            else if (packet.GetType().Equals(typeof(PingResponse)))
            {
                long elapsedTime = currentPingStopWatch.ElapsedMilliseconds;
                currentPingStopWatch.Reset();
                nextPingStopWatch.Restart();

                Ping = elapsedTime / 2;
                RTT = elapsedTime;
                return;
            }
            else if (packet.GetType().Equals(typeof(CloseRequest)))
            {
                CloseRequest closeRequest = (CloseRequest)packet;
                ExternalClose(closeRequest.CloseReason);
                return;
            }
            else if (packet.GetType().Equals(typeof(EstablishUdpRequest)))
            {
                IPEndPoint localEndpoint = new IPEndPoint(IPAddress.IPv6Any, GetFreePort());
                EstablishUdpRequest establishUdpRequest = (EstablishUdpRequest)packet;
                Send(new EstablishUdpResponse(localEndpoint.Port, establishUdpRequest));
                UdpConnection udpConnection = CreateUdpConnection(localEndpoint,
                    new IPEndPoint(IPRemoteEndPoint.Address, establishUdpRequest.UdpPort), true);
                pendingUDPConnections.Enqueue(udpConnection);
                connectionEstablished?.Invoke((TcpConnection)this, udpConnection);
                return;
            }
            else if (packet.GetType().Equals(typeof(EstablishUdpResponseACK)))
            {
                UdpConnection udpConnection = null;
                while (!pendingUDPConnections.TryDequeue(out udpConnection))
                    Thread.Sleep(IntPerformance);
                udpConnection.AcknowledgePending = false;
                return;
            }
            else if (packet.GetType().Equals(typeof(AddPacketTypeRequest)))
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == ((AddPacketTypeRequest)packet).AssemblyName).SingleOrDefault();
                if (assembly == null) CloseHandler(CloseReason.AssemblyDoesNotExist);
                else AddExternalPackets(assembly);
                Send(new AddPacketTypeResponse(typeByte.Values.ToList(), (AddPacketTypeRequest)packet));
                return;
            }
            else if (packet.GetType().Equals(typeof(AddPacketTypeResponse)))
            {
                List<Tuple<Packet, object>> internalSendQueue = new List<Tuple<Packet, object>>();
                AddPacketTypeResponse addPacketTypeResponse = (AddPacketTypeResponse)packet;
                //Remove all packets of this type and send them :)
                while (true)
                {
                    Tuple<Packet, object> toSend = null;
                    while (!pendingUnknownPackets.TryPeek(out toSend) && pendingUnknownPackets.Count > 0)
                        Thread.Sleep(IntPerformance); //Wait till we got a result.

                    //If the other connection contains that packet, send it.
                    if (toSend != null && addPacketTypeResponse.LocalDict.Contains(typeByte[toSend.Item1.GetType()]))
                    {
                        while (!pendingUnknownPackets.TryDequeue(out toSend))
                            Thread.Sleep(IntPerformance); //Wait till we got a result.
                        internalSendQueue.Add(new Tuple<Packet, object>(toSend.Item1, toSend.Item2));
                        continue;
                    }

                    //Now the pendingUnknownPackets queue doesn't contain those elements any more.
                    internalSendQueue.ForEach(i => Send(i.Item1, i.Item2));
                    return;
                }
            }
            //Receiving raw data from the connection.
            else if (packet.GetType().Equals(typeof(RawData)))
            {
                RawData rawData = (RawData)packet;
                if (packetHandlerMap[rawData.Key] == null)
                    Logger.Log($"RawData packet has no listener. Key: {rawData.Key}", LogLevel.Warning);
                else packetHandlerMap[rawData.Key].DynamicInvoke(new object[] { packet, this });
                return;
            }

            try
            {
                if (packet.GetType().IsSubclassOf(typeof(ResponsePacket)) && packetHandlerMap[packet.ID] != null)
                    packetHandlerMap[packet.ID].DynamicInvoke(new object[] { packet, this });
                else if (packetHandlerMap[packet.GetType()] != null)
                    packetHandlerMap[packet.GetType()].DynamicInvoke(new object[] { packet, this });
                else PacketWithoutHandlerReceived(packet);
            }
            catch (Exception exception)
            {
                Logger.Log("Provided delegate contains a bug. Packet invocation thread crashed.", exception, LogLevel.Exception);
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
                receivedUnknownPacketHandlerPackets.Add(packet);
            else Logger.Log($"PacketBuffer exeeded the limit of {PacketBuffer}. " +
                $"Received packet {packet.GetType().Name} dropped.", LogLevel.Error);
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
            //Retreive the packettype for the given handler.
            Type delegateForPacketType = del.GetType().GenericTypeArguments.FirstOrDefault();

            if (receivedUnknownPacketHandlerPackets.Any(p => p.GetType().Equals(delegateForPacketType)))
            {
                var forwardToDelegatePackets = receivedUnknownPacketHandlerPackets.Where(p => p.GetType().Equals(delegateForPacketType));

                foreach (Packet currentForwardPacket in forwardToDelegatePackets)
                {
                    Logger.Log($"Buffered packet {currentForwardPacket.GetType().Name} received a handler => Forwarding", LogLevel.Information);
                    receivedUnknownPacketHandlerPackets.Remove(currentForwardPacket);
                    HandleDefaultPackets(currentForwardPacket);
                }
            }
        }

#endregion Handling Packets

#region Closing The Connection

        /// <summary>
        /// Handles a <see cref="Connection"/> closure, with the given <see cref="CloseReason"/>.
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> closing.</param>
        protected abstract void CloseHandler(CloseReason closeReason);

        /// <summary>
        /// Closes the socket and frees all associated resources.
        /// </summary>
        protected abstract void CloseSocket();

        /// <summary>
        /// Handles a case where the remote <see cref="Connection"/> caused a closure for the given <see cref="CloseReason"/>.
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> closing.</param>
        internal void ExternalClose(CloseReason closeReason)
        {
            networkConnectionClosed?.Invoke(closeReason, this);
            connectionClosed?.Invoke(closeReason, this);

            // close all sockets (throw an exception during any read or write operation)
            CloseSocket();

            // singal all threads to exit their routine.
            threadCancellationTokenSource.Cancel();
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
                Send(new CloseRequest(closeReason));
                WriteSubWork(); //Force to write the remaining packets.
            }
            catch (Exception exception)
            {
                Logger.Log($"Couldn't send a close-message '{closeReason.ToString()}' to the endpoint.", exception, LogLevel.Warning);
            }

            // always inform the internal network lib about the lost connection.
            networkConnectionClosed?.Invoke(closeReason, this);

            if (callCloseEvent)
                connectionClosed?.Invoke(closeReason, this);

            // close all sockets (throw an exception during any read or write operation)
            CloseSocket();

            // singal all threads to exit their routine.
            threadCancellationTokenSource.Cancel();
        }

#endregion Closing The Connection

        /// <summary>
        /// Unlocks the connection and allows for data to be sent and received.
        /// </summary>
        [Obsolete("Unlocking a connection isn't required anymore.")]
        public void UnlockRemoteConnection() => Logger.Log($"UnlockRemoteConnection will be removed in a future release.", LogLevel.Warning);

        /// <summary>
        /// Gets a free port that is currently not in use and returns it.
        /// </summary>
        /// <returns>The port found.</returns>
        protected int GetFreePort()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.IPv6Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Creates and returns a new <see cref="UdpConnection"/>, with the given local endpoint, remote endpoint, and write lock state.
        /// </summary>
        /// <param name="remoteEndPoint">The remote <see cref="IPEndPoint"/> that the <see cref="UdpConnection"/> talks to.</param>
        /// <param name="writeLock">Whether the <see cref="UdpConnection"/> has a write lock.</param>
        /// <returns>The instantiated <see cref="UdpConnection"/>.</returns>
        protected virtual UdpConnection CreateUdpConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, bool writeLock) =>
            new UdpConnection(localEndPoint, remoteEndPoint, writeLock);

#endregion Methods

        /// <summary>
        /// Returns The unique hashcode of this <see cref="Connection"/> instance.
        /// </summary>
        /// <returns> A unique hashcode, suitable for use in hashing algorithms and data structures like a hash table. </returns>
        public override int GetHashCode() => hashCode;

        /// <summary>
        /// Gets the <see cref="string"/> representation of this <see cref="Connection"/> instance.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of this <see cref="Connection"/> instance.</returns>
        public override string ToString() => $"Local: {IPLocalEndPoint?.ToString()} Remote: {IPRemoteEndPoint?.ToString()}";
    }
}