using Network.Enums;
using Network.Interfaces;
using Network.Packets;
using Network.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Network
{
    /// <summary>
    /// Provides convenient methods to reduce the number of code lines which are needed to manage all the connections.
    /// By default one tcp and one udp connection will be created automatically.
    /// </summary>
    public class ClientConnectionContainer : ConnectionContainer, IPacketHandler, IDisposable
    {
        #region Variables

        /// <summary>
        /// The reconnect timer. Invoked if we lose the connection.
        /// </summary>
        private Timer reconnectTimer;

        /// <summary>
        /// The <see cref="Network.TcpConnection"/> for this <see cref="ClientConnectionContainer"/>.
        /// </summary>
        private TcpConnection tcpConnection;

        /// <summary>
        /// The <see cref="Network.UdpConnection"/> for this <see cref="ClientConnectionContainer"/>.
        /// </summary>
        private UdpConnection udpConnection;

        #region TCP Transmission Variables

        /// <summary>
        /// Buffer for all messages to be sent via TCP to the remote <see cref="Connection"/>, once the connection is established.
        /// </summary>
        private readonly List<Packet> sendSlowBuffer = new List<Packet>();

        /// <summary>
        /// Buffer for all messages to be sent via TCP to the remote <see cref="Connection"/>, once the connection is established, that
        /// have a sender instance who is waiting for a response.
        /// </summary>
        private readonly List<Tuple<Packet, object>> sendSlowObjectBuffer = new List<Tuple<Packet, object>>();

        /// <summary>
        /// Cache of all the <see cref="PacketReceivedHandler{P}"/>s to register on the remote <see cref="Network.TcpConnection"/>
        /// once a connection is established.
        /// </summary>
        private PacketHandlerMap tcpPacketHandlerBackup = new PacketHandlerMap();

        /// <summary>
        /// Buffer for all the static packet handlers to register on the remote <see cref="Network.TcpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, Delegate>> tcpStaticPacketHandlerBuffer = new List<Tuple<Type, Delegate>>();

        /// <summary>
        /// Buffer for all the static packet handlers to deregister on the remote <see cref="Network.TcpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Type> tcpStaticUnPacketHandlerBuffer = new List<Type>();

        /// <summary>
        /// Buffer for all the packet handlers to register on the remote <see cref="Network.TcpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, Delegate, object>> tcpPacketHandlerBuffer = new List<Tuple<Type, Delegate, object>>();

        /// <summary>
        /// Buffer for all the packet handlers to deregister on the remote <see cref="Network.TcpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, object>> tcpUnPacketHandlerBuffer = new List<Tuple<Type, object>>();

        #endregion TCP Transmission Variables

        #region UDP Transmission Variables

        /// <summary>
        /// Buffer for all messages to be sent via UDP to the remote <see cref="Connection"/>, once the connection is established.
        /// </summary>
        private readonly List<Packet> sendFastBuffer = new List<Packet>();

        /// <summary>
        /// Buffer for all messages to be sent via UDP to the remote <see cref="Connection"/>, once the connection is established, that
        /// have a sender instance who is waiting for a response.
        /// </summary>
        private readonly List<Tuple<Packet, object>> sendFastObjectBuffer = new List<Tuple<Packet, object>>();

        /// <summary>
        /// Cache of all the <see cref="PacketReceivedHandler{P}"/>s to register on the remote <see cref="Network.UdpConnection"/>
        /// once a connection is established.
        /// </summary>
        private PacketHandlerMap udpPacketHandlerBackup = new PacketHandlerMap();

        /// <summary>
        /// Buffer for all the static packet handlers to register on the remote <see cref="Network.UdpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, Delegate>> udpStaticPacketHandlerBuffer = new List<Tuple<Type, Delegate>>();

        /// <summary>
        /// Buffer for all the static packet handlers to deregister on the remote <see cref="Network.UdpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Type> udpStaticUnPacketHandlerBuffer = new List<Type>();

        /// <summary>
        /// Buffer for all the packet handlers to register on the remote <see cref="Network.UdpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, Delegate, object>> udpPacketHandlerBuffer = new List<Tuple<Type, Delegate, object>>();

        /// <summary>
        /// Buffer for all the packet handlers to deregister on the remote <see cref="Network.UdpConnection"/> once
        /// a connection is established.
        /// </summary>
        private readonly List<Tuple<Type, object>> udpUnPacketHandlerBuffer = new List<Tuple<Type, object>>();

        #endregion UDP Transmission Variables

        // TODO Remove all occurrences of backing fields for events in favor of new, cleaner 'event?.Invoke(args)' syntax

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<Connection, ConnectionType, CloseReason> connectionLost;

        /// <summary>
        /// A handler which will be invoked if a new connection is established.
        /// </summary>
        private event Action<Connection, ConnectionType> connectionEstablished;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The remote ip address.</param>
        /// <param name="port">The remote port.</param>
        internal ClientConnectionContainer(string ipAddress, int port)
            : base(ipAddress, port) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection to use.</param>
        /// <param name="udpConnection">The UDP connection to use.</param>
        internal ClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection)
            : base(tcpConnection.IPRemoteEndPoint.Address.ToString(), tcpConnection.IPRemoteEndPoint.Port)
        {
            this.tcpConnection = tcpConnection;
            this.udpConnection = udpConnection;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Whether to automatically attempt to reconnect to the remote endpoint once the connection is lost.
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// The interval in milliseconds between reconnect attempts.
        /// </summary>
        public int ReconnectInterval
        {
            get => (int)reconnectTimer.Interval;
            set => reconnectTimer.Interval = value;
        }

        /// <summary>
        /// The current <see cref="Network.TcpConnection"/> for this instance.
        /// </summary>
        public TcpConnection TcpConnection { get { return tcpConnection; } }

        /// <summary>
        /// The current <see cref="Network.UdpConnection"/> for this instance.
        /// </summary>
        public UdpConnection UdpConnection { get { return udpConnection; } }

        /// <summary>
        /// Whether the <see cref="TcpConnection"/> is currently alive.
        /// </summary>
        public bool IsAlive_TCP
        {
            get
            {
                if (tcpConnection == null)
                    return false;
                return tcpConnection.IsAlive;
            }
        }

        /// <summary>
        /// Whether the <see cref="UdpConnection"/> is currently alive.
        /// </summary>
        public bool IsAlive_UDP
        {
            get
            {
                if (udpConnection == null)
                    return false;
                return udpConnection.IsAlive;
            }
        }

        /// <summary>
        /// Whether both the <see cref="TcpConnection"/> and <see cref="UdpConnection"/> are currently alive.
        /// </summary>
        public bool IsAlive { get { return IsAlive_TCP && IsAlive_UDP; } }

        #endregion Properties

        #region Events

        /// <summary>
        /// Signifies that a connection has been made on either the <see cref="TcpConnection"/> or <see cref="UdpConnection"/>.
        /// </summary>
        public event Action<Connection, ConnectionType> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

        /// <summary>
        /// Signifies that a connection has been lost on either the <see cref="TcpConnection"/> or <see cref="UdpConnection"/>.
        /// </summary>
        public event Action<Connection, ConnectionType, CloseReason> ConnectionLost
        {
            add { connectionLost += value; }
            remove { connectionLost -= value; }
        }

        #endregion Events

        #region Methods

        #region Overrides of Object

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ClientConnectionContainer. TCP is alive {IsAlive_TCP}. UDP is alive {IsAlive_UDP}. Server IPAddress {IPAddress} Port {Port.ToString()}";
        }

        #endregion Overrides of Object

        /// <summary>
        /// Initialises this <see cref="ClientConnectionContainer"/> instance and attempts to connect to the current <see cref="Connection.IPRemoteEndPoint"/>.
        /// </summary>
        internal void Initialize()
        {
            reconnectTimer = new Timer();
            ReconnectInterval = 2500;
            reconnectTimer.Interval = ReconnectInterval;
            reconnectTimer.Elapsed += TryToConnect;
            TryConnect();
        }

        /// <summary>
        /// Tries to connect to the given endpoint.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">Any event arguments.</param>
        private void TryToConnect(object sender, ElapsedEventArgs e) => TryConnect();

        /// <summary>
        /// Tries to connect to the current <see cref="Connection.IPRemoteEndPoint"/>.
        /// </summary>
        private async void TryConnect()
        {
            if (reconnectTimer != null)
                reconnectTimer.Stop();

            if (tcpConnection == null || !tcpConnection.IsAlive)
                await OpenNewTCPConnection();
            if ((udpConnection == null || !udpConnection.IsAlive) && IsAlive_TCP)
                await OpenNewUDPConnection();
        }

        /// <summary>
        /// Closes the <see cref="TcpConnection"/> and <see cref="UdpConnection"/> with the given <see cref="CloseReason"/>,
        /// optionally calling the <see cref="Connection.ConnectionClosed"/> event.
        /// </summary>
        /// <param name="closeReason">The reason for connection closure.</param>
        /// <param name="callCloseEvent">Whether to call the <see cref="Connection.ConnectionClosed"/> event.</param>
        public void Shutdown(CloseReason closeReason, bool callCloseEvent = false)
        {
            if (IsAlive_TCP) tcpConnection.Close(closeReason, callCloseEvent);
            if (IsAlive_UDP) udpConnection.Close(closeReason, callCloseEvent);
        }

        #region Implmentation of IPacketHandler for TCP and UDP

        /// <inheritdoc cref="IPacketHandler.RegisterStaticPacketHandler{P}"/>
        public void TCP_RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.RegisterStaticPacketHandler<P>(handler);
            else tcpStaticPacketHandlerBuffer.Add(new Tuple<Type, Delegate>(typeof(P), handler));
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterStaticPacketHandler{P}"/>
        public void TCP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterStaticPacketHandler<P>();
            else tcpStaticUnPacketHandlerBuffer.Add(typeof(P));
        }

        /// <inheritdoc cref="IPacketHandler.RegisterPacketHandler{P}"/>
        public void TCP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.RegisterPacketHandler<P>(handler, obj);
            else tcpPacketHandlerBuffer.Add(new Tuple<Type, Delegate, object>(typeof(P), handler, obj));
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterPacketHandler{P}"/>
        public void TCP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterPacketHandler<P>(obj);
            else tcpUnPacketHandlerBuffer.Add(new Tuple<Type, object>(typeof(P), obj));
        }

        /// <inheritdoc cref="IPacketHandler.RegisterStaticPacketHandler{P}"/>
        public void UDP_RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterStaticPacketHandler<P>(handler);
            else udpStaticPacketHandlerBuffer.Add(new Tuple<Type, Delegate>(typeof(P), handler));
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterStaticPacketHandler{P}"/>
        public void UDP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterStaticPacketHandler<P>();
            else udpStaticUnPacketHandlerBuffer.Add(typeof(P));
        }

        /// <inheritdoc cref="IPacketHandler.RegisterPacketHandler{P}"/>
        public void UDP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterPacketHandler<P>(handler, obj);
            else udpPacketHandlerBuffer.Add(new Tuple<Type, Delegate, object>(typeof(P), handler, obj));
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterPacketHandler{P}"/>
        public void UDP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterPacketHandler<P>(obj);
            else udpUnPacketHandlerBuffer.Add(new Tuple<Type, object>(typeof(P), obj));
        }

        /// <inheritdoc />
        public void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            TCP_RegisterStaticPacketHandler<P>(handler);
            UDP_RegisterStaticPacketHandler<P>(handler);
        }

        /// <inheritdoc />
        public void UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            TCP_UnRegisterStaticPacketHandler<P>();
            UDP_UnRegisterStaticPacketHandler<P>();
        }

        /// <inheritdoc />
        public void RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            TCP_RegisterPacketHandler<P>(handler, obj);
            UDP_RegisterPacketHandler<P>(handler, obj);
        }

        /// <inheritdoc />
        public void UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            TCP_UnRegisterPacketHandler<P>(obj);
            UDP_UnRegisterPacketHandler<P>(obj);
        }

        #endregion Implmentation of IPacketHandler for TCP and UDP

        #region Opening New Connections

        /// <summary>
        /// Opens the new TCP connection and applies any buffered (i.e. already registered) packet handlers.
        /// </summary>
        private async Task OpenNewTCPConnection()
        {
            Tuple<TcpConnection, ConnectionResult> result = await CreateTcpConnection();

            if (result.Item2 != ConnectionResult.Connected)
            {
                Reconnect();
                return;
            }

            tcpConnection = result.Item1;

            //Restore old state by adding old packets
            tcpConnection.RestorePacketHandler(tcpPacketHandlerBackup);
            //Restore new state by adding packets the user wanted to register while the connection was dead.
            tcpPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(tcpConnection, new object[] { t.Item2, t.Item3 });
            });
            tcpStaticPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterStaticPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(tcpConnection, new object[] { t.Item2 });
            });

            sendSlowBuffer.ForEach(tcpConnection.Send);
            sendSlowObjectBuffer.ForEach(p => tcpConnection.Send(p.Item1, p.Item2));
            //Restore new state by removing the packets the user wanted to unregister while the connection was dead.
            tcpUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t.Item1);
                unRegisterPacketHandler.Invoke(tcpConnection, new object[] { t.Item2 });
            });
            tcpStaticUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterStaticPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t);
                unRegisterPacketHandler.Invoke(tcpConnection, null);
            });

            KnownTypes.ForEach(TcpConnection.AddExternalPackets);
            //Clear the buffers since we added and removed the packet types.
            sendSlowBuffer.Clear();
            sendSlowObjectBuffer.Clear();
            tcpPacketHandlerBuffer.Clear();
            tcpUnPacketHandlerBuffer.Clear();
            tcpStaticPacketHandlerBuffer.Clear();
            tcpStaticUnPacketHandlerBuffer.Clear();

            //Connection could already be dead because of the prePackets.
            if (!tcpConnection.IsAlive)
            {
                tcpPacketHandlerBackup = tcpConnection.BackupPacketHandler();
                connectionLost?.Invoke(tcpConnection, ConnectionType.TCP, tcpConnection.CloseReason);
                Reconnect();
            }
            else
            {
                tcpConnection.ConnectionClosed += (c, cc) =>
                {
                    tcpPacketHandlerBackup = cc.BackupPacketHandler();
                    connectionLost?.Invoke(tcpConnection, ConnectionType.TCP, c);
                    Reconnect();
                };

                connectionEstablished?.Invoke(tcpConnection, ConnectionType.TCP);
            }
        }

        /// <summary>
        /// Opens the new UDP connection and applies any buffered (i.e. already registered) packet handlers.
        /// </summary>
        private async Task OpenNewUDPConnection()
        {
            Tuple<UdpConnection, ConnectionResult> result = await CreateUdpConnection();

            if (result.Item2 != ConnectionResult.Connected)
            {
                Reconnect();
                return;
            }

            udpConnection = result.Item1;
            //Restore old state by adding old packets
            udpConnection.RestorePacketHandler(udpPacketHandlerBackup);
            //Restore new state by adding packets the user wanted to register while the connection was dead.
            udpPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(udpConnection, new object[2] { t.Item2, t.Item3 });
            });
            udpStaticPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterStaticPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(udpConnection, new object[] { t.Item2 });
            });

            sendFastBuffer.ForEach(udpConnection.Send);
            sendFastObjectBuffer.ForEach(p => udpConnection.Send(p.Item1, p.Item2));
            //Restore new state by removing the packets the user wanted to unregister while the connection was dead.
            udpUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t.Item1);
                unRegisterPacketHandler.Invoke(udpConnection, new object[] { t.Item2 });
            });
            udpStaticUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterStaticPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t);
                unRegisterPacketHandler.Invoke(udpConnection, null);
            });

            KnownTypes.ForEach(UdpConnection.AddExternalPackets);
            //Clear the buffers since we added and removed the packet types.
            sendFastBuffer.Clear();
            sendFastObjectBuffer.Clear();
            udpPacketHandlerBuffer.Clear();
            udpUnPacketHandlerBuffer.Clear();
            udpStaticPacketHandlerBuffer.Clear();
            udpStaticUnPacketHandlerBuffer.Clear();

            //Connection could already be dead because of the prePackets.
            if (!UdpConnection.IsAlive)
            {
                udpPacketHandlerBackup = udpConnection.BackupPacketHandler();
                connectionLost?.Invoke(udpConnection, ConnectionType.UDP, udpConnection.CloseReason);
                Reconnect();
            }
            else
            {
                udpConnection.ConnectionClosed += (c, cc) =>
                {
                    udpPacketHandlerBackup = cc.BackupPacketHandler();
                    connectionLost?.Invoke(udpConnection, ConnectionType.UDP, c);
                    Reconnect();
                };

                connectionEstablished?.Invoke(udpConnection, ConnectionType.UDP);
            }
        }

        #endregion Opening New Connections

        #region Sending Packets

        /// <summary>
        /// Sends a ping over the TCP connection.
        /// </summary>
        public void SendPing()
        {
            if (tcpConnection != null && !tcpConnection.IsAlive)
                sendSlowBuffer.Add(new PingRequest());
            else tcpConnection.SendPing();
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> to the network via TCP. The sender will not receive an answer, due to
        /// no sender instance being given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void Send(Packet packet)
        {
            SendSlow(packet);
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> to the network, via the given <see cref="ConnectionType"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="type">The connection type to use.</param>
        /// <exception cref="ArgumentException">Thrown when the given <see cref="ConnectionType"/> value is an invalid cast.</exception>
        public void Send(Packet packet, ConnectionType type)
        {
            if (type == ConnectionType.TCP)
                SendSlow(packet);
            else if (type == ConnectionType.UDP)
                SendFast(packet);
            else
                throw new ArgumentException("The given enum doesn't exist");
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> over the network via TCP and awaits a <see cref="ResponsePacket"/> on the
        /// given <see cref="object"/> instance.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The sender instance to receive a response.</param>
        public void Send(Packet packet, object instance)
        {
            SendSlow(packet, instance);
        }

        /// <summary>
        /// Asynchronously sends the given <see cref="Packet"/> over the network via TCP and awaits a <see cref="ResponsePacket"/>
        /// of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ResponsePacket"/> to await.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, with the promise of the received
        /// <see cref="ResponsePacket"/> on completion.
        /// </returns>
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket
        {
            return await SendAsync<T>(packet, ConnectionType.TCP);
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> over the network via the given <see cref="ConnectionType"/>
        /// and awaits a <see cref="ResponsePacket"/> on the given <see cref="object"/> instance.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The sender instance to receive a response.</param>
        /// <param name="type">The connection type to use.</param>
        /// <exception cref="ArgumentException">Thrown when the given <see cref="ConnectionType"/> value is an invalid cast.</exception>
        public void Send(Packet packet, object instance, ConnectionType type)
        {
            if (type == ConnectionType.TCP)
                SendSlow(packet, instance);
            else if (type == ConnectionType.UDP)
                SendFast(packet, instance);
            else
                throw new ArgumentException("The given enum doesn't exist");
        }

        /// <summary>
        /// Asynchronously sends the given <see cref="Packet"/> over the network via the given <see cref="ConnectionType"/>
        /// and awaits a <see cref="ResponsePacket"/> of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ResponsePacket"/> to await.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <param name="connectionType">The connection type to use.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, with the promise of the received
        /// <see cref="ResponsePacket"/> on completion.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the given <see cref="ConnectionType"/> value is an invalid cast.</exception>
        public async Task<T> SendAsync<T>(Packet packet, ConnectionType connectionType) where T : ResponsePacket
        {
            if (connectionType == ConnectionType.TCP)
                return await SendSlowAsync<T>(packet);
            else if (connectionType == ConnectionType.UDP)
                return await SendFastAsync<T>(packet);
            else
                throw new ArgumentException("The given enum doesn't exist");
        }

        #region Sending via TCP

        /// <summary>
        /// Sends the given <see cref="Packet"/> to the network via TCP. The sender will not receive an answer, due to
        /// no sender instance being given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendSlow(Packet packet)
        {
            if (tcpConnection == null || !tcpConnection.IsAlive)
                sendSlowBuffer.Add(packet);
            else tcpConnection.Send(packet);
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> over the network via TCP and awaits a <see cref="ResponsePacket"/> on the
        /// given <see cref="object"/> instance.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The sender instance to receive a response.</param>
        public void SendSlow(Packet packet, object instance)
        {
            if (IsAlive_TCP) tcpConnection.Send(packet, instance);
            else sendSlowObjectBuffer.Add(new Tuple<Packet, object>(packet, instance));
        }

        /// <summary>
        /// Asynchronously sends the given <see cref="Packet"/> over the network via TCP and awaits a <see cref="ResponsePacket"/>
        /// of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ResponsePacket"/> to await.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, with the promise of the received
        /// <see cref="ResponsePacket"/> on completion.
        /// </returns>
        public async Task<T> SendSlowAsync<T>(Packet packet) where T : ResponsePacket
        {
            if (IsAlive_TCP) return await tcpConnection.SendAsync<T>(packet);
            T response = Activator.CreateInstance<T>();
            response.State = PacketState.ConnectionNotAlive;
            return response;
        }

        #endregion Sending via TCP

        #region Sending via UDP

        /// <summary>
        /// Sends the given <see cref="Packet"/> to the network via UDP. The sender will not receive an answer, due to
        /// no sender instance being given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendFast(Packet packet)
        {
            if (udpConnection == null || !udpConnection.IsAlive)
                sendFastBuffer.Add(packet);
            else udpConnection.Send(packet);
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> over the network via UDP and awaits a <see cref="ResponsePacket"/> on the
        /// given <see cref="object"/> instance.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The sender instance to receive a response.</param>
        public void SendFast(Packet packet, object instance)
        {
            if (IsAlive_UDP) udpConnection.Send(packet, instance);
            else sendFastObjectBuffer.Add(new Tuple<Packet, object>(packet, instance));
        }

        /// <summary>
        /// Asynchronously sends the given <see cref="Packet"/> over the network via UDP and awaits a <see cref="ResponsePacket"/>
        /// of the given type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ResponsePacket"/> to await.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, with the promise of the received
        /// <see cref="ResponsePacket"/> on completion.
        /// </returns>
        public async Task<T> SendFastAsync<T>(Packet packet) where T : ResponsePacket
        {
            if (IsAlive_UDP) return await udpConnection.SendAsync<T>(packet);
            T response = Activator.CreateInstance<T>();
            response.State = PacketState.ConnectionNotAlive;
            return response;
        }

        #endregion Sending via UDP

        #endregion Sending Packets

        /// <summary>
        /// Reconnects both the <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="forceReconnect">Whether to ignore the <see cref="AutoReconnect"/> value and forcibly reconnect.</param>
        public void Reconnect(bool forceReconnect = false)
        {
            reconnectTimer.Stop();

            if (forceReconnect || AutoReconnect)
                reconnectTimer.Start();
        }

        /// <summary>
        /// Creates a new <see cref="Network.TcpConnection"/>.
        /// </summary>
        /// <returns>The created <see cref="Network.TcpConnection"/>.</returns>
        protected virtual async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnection() =>
            await ConnectionFactory.CreateTcpConnectionAsync(IPAddress, Port);

        /// <summary>
        /// Creates a new <see cref="Network.UdpConnection"/>.
        /// </summary>
        /// <returns>The created <see cref="Network.UdpConnection"/>.</returns>
        protected virtual async Task<Tuple<UdpConnection, ConnectionResult>> CreateUdpConnection() =>
            await ConnectionFactory.CreateUdpConnectionAsync(tcpConnection);

        #endregion Methods

        #region Implmentation of IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            reconnectTimer.Elapsed -= TryToConnect;
            reconnectTimer.Stop();
            reconnectTimer.Dispose();
        }

        #endregion Implmentation of IDisposable
    }
}