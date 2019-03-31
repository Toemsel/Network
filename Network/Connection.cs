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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// Provides the basic methods a connection has to implement.
    /// It ensures the connectivity, is able to send pings and keeps track of the latency.
    /// Every connection instance has 3 threads:
    /// - (1) Send thread       -> Writes enqueued packets on the stream
    /// - (2) Read thread       -> Read bytes from the stream
    /// - (3) Invoke thread     -> Delegates the received packets to the given delegate.
    /// All 3 threads will be automatically aborted if the connection has been closed.
    /// After closing the connection, every packet in the send queue will be send before closing the connection.
    /// </summary>
    public abstract partial class Connection : IPacketHandler
    {
        #region Variables

        /// <summary>
        /// A fixed hashcode that persists with the <see cref="Connection"/> instance for its entire lifetime.
        /// </summary>
        private readonly int hashCode;

        /// <summary>
        /// Constants.
        /// </summary>
        protected const int PING_INTERVALL = 10000;

        /// <summary>
        /// True if this instance should send in a specific interval a keep alive packet, to ensure
        /// whether there is a connection or not. If set to [false] <see cref="RTT"/> and <see cref="Ping"/> wont be enabled/refreshed.
        /// </summary>
        private bool keepAlive = false;

        /// <summary>
        /// Is able to convert a packet into a byte array and back.
        /// </summary>
        private IPacketConverter packetConverter = new PacketConverter();

        private ConcurrentQueue<UdpConnection> pendingUDPConnections = new ConcurrentQueue<UdpConnection>();
        private ConcurrentQueue<Tuple<Packet, object>> pendingUnknownPackets = new ConcurrentQueue<Tuple<Packet, object>>();

        /// <summary>
        /// When this stopwatch reached the <see cref="ReceiveTimeout"/> the instance is going to send a ping request.
        /// </summary>
        private Stopwatch nextPingStopWatch = new Stopwatch();

        private Stopwatch currentPingStopWatch = new Stopwatch();

        /// <summary>
        /// This concurrent queue contains the received/send packets which we have to handle.
        /// </summary>
        private ConcurrentQueue<Packet> receivedPackets = new ConcurrentQueue<Packet>();

        private ConcurrentQueue<Tuple<Packet, object>> sendPackets = new ConcurrentQueue<Tuple<Packet, object>>();
        private ConcurrentBag<Packet> receivedUnknownPacketHandlerPackets = new ConcurrentBag<Packet>();

        /// <summary>
        /// Events to save CPU time.
        /// </summary>
        private AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);

        private AutoResetEvent packetAvailableEvent = new AutoResetEvent(false);

        #region Threads

        private Thread readStreamThread;
        private Thread writeStreamThread;
        private Thread invokePacketThread;

        #endregion Threads

        /// <summary>
        /// Maps the type of a packet to their byte value.
        /// </summary>
        private BiDictionary<Type, ushort> typeByte = new BiDictionary<Type, ushort>();

        private int currentTypeByteIndex = 100; //The current index we are facing. Start with 100, since we have some network packets.

        /// <summary>
        /// Maps a request to their response.
        /// </summary>
        private static readonly Dictionary<Type, Type> requestResponseMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Has to map the objects to their unique id and back.
        /// </summary>
        private readonly PacketHandlerMap packetHandlerMap = new PacketHandlerMap();

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

        #region Socket Variables

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

        #endregion Socket Variables

        /// <summary>
        /// Whether this <see cref="Connection"/> should send a keep alive packet to the <see cref="RemoteIPEndPoint"/> at
        /// specific intervals, to ensure whether there is a remote <see cref="Connection"/> or not. If set to <c>false</c>
        /// <see cref="RTT"/> and <see cref="Ping"/> wont be refreshed automatically.
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
        public virtual long RTT { get; protected set; } = 0;

        /// <summary>
        /// The ping to the <see cref="RemoteIPEndPoint"/>.
        /// </summary>
        public virtual long Ping { get; protected set; } = 0;

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
        public override string ToString() => $"Local: {LocalIPEndPoint?.ToString()} Remote: {RemoteIPEndPoint?.ToString()}";

        #endregion Overrides of Object

        #region Implementation of IPacketHandler

        /// <inheritdoc />
        public void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            packetHandlerMap.RegisterStaticPacketHandler<P>(handler);
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
            SearchAndInvokeUnknownHandlerPackets((Delegate)handler);
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
            SearchAndInvokeUnknownHandlerPackets((Delegate)handler);
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
        /// External packets which also should be known by the network lib can be added with this function.
        /// All packets in the network lib are included automatically. A call is not essential, even if the used packets
        /// are not included in the network library. Manuell calls have to be invoked on the client and server side to avaid incompatible states.
        /// </summary>
        /// <param name="assembly">The assembly to search for included packets.</param>
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
                    if (!requestResponseMap.ContainsKey(requestAttribute.RequestType))
                        requestResponseMap.Add(requestAttribute.RequestType, c);
                });
        }

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
        /// Restores the packetHandler. Can only be called if the internal packetHandler is empty.
        /// </summary>
        /// <param name="packetHandlerMap">The object map to restore.</param>
        internal void RestorePacketHandler(PacketHandlerMap packetHandlerMap)
        {
            this.packetHandlerMap.Restore(packetHandlerMap);
            ObjectMapRefreshed();
        }

        /// <summary>
        /// Configurations the ping and rtt timers.
        /// </summary>
        private void ConfigPing(bool enable)
        {
#if !DEBUG
            if (enable) nextPingStopWatch.Restart();
            else nextPingStopWatch.Reset();
#endif
        }

        #region Sending Packets

        /// <summary>
        /// Sends a ping if there is no ping request already running.
        /// </summary>
        public void SendPing()
        {
            if (currentPingStopWatch.IsRunning) return;
            nextPingStopWatch.Reset();
            currentPingStopWatch.Restart();
            Send(new PingRequest());
        }

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// You wont be able to receive an answer, because no calling object is given.
        /// Suitable for static classes, response packets or basic packets without any inheritance.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public void Send(Packet packet) => Send(packet, null);

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// You are able to receive an answer. Iff the packet you send is a request packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="instance">The instance who called this method.</param>
        public void Send(Packet packet, object instance) => Send(packet, instance, false);

        /// <summary>
        /// Converts the given packet into a binary array and sends it async to the client's endpoint.
        /// You are able to receive an answer. Iff the packet you send is a request packet.
        /// </summary>
        /// <typeparam name="T">The type of the expected answer.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>T.</returns>
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket => await new ChickenReceiver().Send<T>(packet, this);

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// You wont be able to receive an answer, because no calling object is given.
        /// Suitable for static classes, response packets or basic packets without any inheritance.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="ignoreWriteLock">if set to <c>true</c> [ignore write lock].</param>
        internal void Send(Packet packet, bool ignoreWriteLock) => Send(packet, null, ignoreWriteLock);

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="ignoreWriteLock">if set to <c>true</c> [ignore write lock].</param>
        internal void Send(Packet packet, object instance, bool ignoreWriteLock)
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

        /// <summary>
        /// Sends raw data.
        /// </summary>
        /// <param name="key">The identifying key.</param>
        /// <param name="data">The data to send.</param>
        public void SendRawData(string key, byte[] data)
        {
            if (data == null)
            {
                Logger.Log("Can't send a null reference data byte array", new ArgumentException());
                return;
            }

            Send(new RawData(key, data));
        }

        /// <summary>
        /// Sends a raw data packet.
        /// </summary>
        /// <param name="rawData">The packet to send.</param>
        public void SendRawData(RawData rawData) => Send(rawData);

        #endregion Sending Packets

        /// <summary>
        /// If a packet has been received which has no receiver (delegate)
        /// it will be stored till a receiver (delegate) joins the party.
        /// This method searches for lonely, stored packets, which had
        /// no receiver in the past, but may have a receiver now. In that
        /// case, we immediately forward the packet to the subscriber
        /// and remove it from the sad, lonely collection.
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

        #region Threads

        /// <summary>
        /// Reads the bytes from the stream.
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

                    Packet receivedPacket = packetConverter.DeserialisePacket(typeByte[packetType], packetData);
                    receivedPackets.Enqueue(receivedPacket);
                    receivedPacket.Size = packetLength;
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
        /// Writes the packets to the stream.
        /// </summary>
        private void WriteWork()
        {
            try
            {
                while (true)
                {
                    //Wait till we have something to send.
                    dataAvailableEvent.WaitOne();

                    WriteSubWork();

                    //Check if the client is still alive.
                    if (KeepAlive && nextPingStopWatch.ElapsedMilliseconds >= PING_INTERVALL)
                    {
                        SendPing();
                    }
                    else if (currentPingStopWatch.ElapsedMilliseconds >= ReceiveTimeout &&
                        currentPingStopWatch.ElapsedMilliseconds != 0)
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
                Logger.Log("Write object on stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.WritePacketThreadException);
        }

        /// <summary>
        /// This thread checks for new packets in the queue and delegates them
        /// to the desired delegates, if given.
        /// </summary>
        private void InvokeWork()
        {
            try
            {
                while (true)
                {
                    //Wait till we receive a packet.
                    packetAvailableEvent.WaitOne();

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
            catch (ThreadAbortException) { return; }
            catch (Exception) { }

            CloseHandler(CloseReason.InvokePacketThreadException);
        }

        #endregion Threads

        /// <summary>
        /// Writes the packets to the stream.
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

                byte[] packetData = packetConverter.SerialisePacket(packet);
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
        /// Handle the network's packets.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
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
                EstablishUdpRequest establishUdpRequest = (EstablishUdpRequest)packet;
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, GetFreePort());
                Send(new EstablishUdpResponse(udpEndPoint.Port, establishUdpRequest));
                UdpConnection udpConnection = CreateUdpConnection(udpEndPoint,
                    new IPEndPoint(RemoteIPEndPoint.Address, establishUdpRequest.UdpPort), true);
                pendingUDPConnections.Enqueue(udpConnection);
                OnConnectionEstablished((TcpConnection)this, udpConnection);
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
        /// Packets the without handler received.
        /// </summary>
        /// <param name="packet">The packet.</param>
        protected virtual void PacketWithoutHandlerReceived(Packet packet)
        {
            Logger.Log($"Packet with no handler received: {packet.GetType().Name}.", LogLevel.Warning);
            if (receivedUnknownPacketHandlerPackets.Count < PacketBuffer)
                receivedUnknownPacketHandlerPackets.Add(packet);
            else Logger.Log($"PacketBuffer exeeded the limit of {PacketBuffer}. " +
                $"Received packet {packet.GetType().Name} dropped.", LogLevel.Error);
        }

        /// <summary>
        /// The remote endpoint closed the connection.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        internal void ExternalClose(CloseReason closeReason)
        {
            writeStreamThread.AbortSave();
            readStreamThread.AbortSave();
            OnConnectionClosed(closeReason, this);
            invokePacketThread.AbortSave();
            CloseSocket();
        }

        /// <summary>
        /// Closes this connection, but still sends the data on the stream to the bound endpoint.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        /// <param name="callCloseEvent">If the instance should call the connectionLost event.</param>
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

        /// <summary>
        /// Unlocks the remote connection so that he is able to send packets.
        /// </summary>
        [Obsolete("Unlocking a connection isn't required anymore.")]
        public void UnlockRemoteConnection() => Logger.Log($"UnlockRemoteConnection will be removed in a future release.", LogLevel.Warning);

        /// <summary>
        /// Gets the next free port.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected int GetFreePort()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Handles the unknown packet.
        /// </summary>
        protected abstract void HandleUnknownPacket();

        /// <summary>
        /// The packetHandlerMap has been refreshed.
        /// </summary>
        public virtual void ObjectMapRefreshed() { }

        /// <summary>
        /// Creates a new UdpConnection.
        /// </summary>
        /// <param name="localEndPoint">The localEndPoint.</param>
        /// <param name="remoteEndPoint">The removeEndPoint.</param>
        /// <param name="writeLock">The writeLock.</param>
        /// <returns>A UdpConnection.</returns>
        protected virtual UdpConnection CreateUdpConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, bool writeLock) => new UdpConnection(new UdpClient(localEndPoint), remoteEndPoint, writeLock);

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        protected abstract byte[] ReadBytes(int amount);

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        protected abstract void WriteBytes(byte[] bytes);

        /// <summary>
        /// Handles if the connection should be closed, based on the reason.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        protected abstract void CloseHandler(CloseReason closeReason);

        /// <summary>
        /// Closes the socket.
        /// </summary>
        protected abstract void CloseSocket();

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