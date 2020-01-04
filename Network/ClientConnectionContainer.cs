using Network.Enums;
using Network.Exceptions;
using Network.Interfaces;
using Network.Packets;
using Network.Utilities;
using System;
using System.Collections.Generic;
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
        private Timer reconnectTimer = new Timer();

        /// <summary>
        /// The <see cref="Network.TcpConnection"/> for this <see cref="ClientConnectionContainer"/>.
        /// </summary>
        private TcpConnection tcpConnection;

        /// <summary>
        /// The <see cref="Network.UdpConnection"/> for this <see cref="ClientConnectionContainer"/>.
        /// </summary>
        private UdpConnection udpConnection;     

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<Connection, ConnectionType, CloseReason> connectionLost;

        /// <summary>
        /// A handler which will be invoked if a new connection is established.
        /// </summary>
        private event Action<Connection, ConnectionType> connectionEstablished;

        private PacketHandlerMap tcpPacketHandlerBackup = new PacketHandlerMap();
        private PacketHandlerMap udpPacketHandlerBackup = new PacketHandlerMap();

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection to use.</param>
        /// <param name="udpConnection">The UDP connection to use.</param>
        internal ClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection)
            : this(tcpConnection.IPRemoteEndPoint.Address.ToString(), tcpConnection.IPRemoteEndPoint.Port)
        {
            this.tcpConnection = tcpConnection;
            this.udpConnection = udpConnection;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The remote ip address.</param>
        /// <param name="port">The remote port.</param>
        internal ClientConnectionContainer(string ipAddress, int port) : base(ipAddress, port) => ReconnectInterval = 2500;

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

        /// <summary>
        /// If the <see cref="Send(Packet)"/> (or any other sending related method) gets called
        /// and the corresponding connection isn't alive, the <see cref="Packet" /> can't be sent to the endpoint.
        /// Hence, the <see cref="Packet" /> is undeliverable. In such a chase, this property indicates
        /// whether to throw an <see cref="ConnectionNotAliveException" />.
        /// </summary>
        /// <value><c>true</c> throws <see cref="ConnectionNotAliveException" /> on a dead connection.</value>
        public bool ThrowExceptionOnUndeliverablePackets { get; set; } = false;

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

        /// <summary>
        /// Initialises this <see cref="ClientConnectionContainer"/> instance and attempts to connect to the current <see cref="Connection.IPRemoteEndPoint"/>.
        /// </summary>
        internal void Initialize()
        {
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
        /// <param name="callCloseEvent">Whether to call the <see cref="Connection.ConnectionClosed"/> event. <c>True</c> the <see cref="ClientConnectionContainer" /> tries to reconnect to it's endpoint afterwards again; Plus, calles the <see cref="Connection.ConnectionClosed" /> event. Otherwise <c>False</c></param>
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
            else tcpPacketHandlerBackup.RegisterStaticPacketHandler<P>(handler);
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterStaticPacketHandler{P}"/>
        public void TCP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterStaticPacketHandler<P>();
            else tcpPacketHandlerBackup.UnRegisterStaticPacketHandler<P>();
        }

        /// <inheritdoc cref="IPacketHandler.RegisterPacketHandler{P}"/>
        public void TCP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.RegisterPacketHandler<P>(handler, obj);
            else tcpPacketHandlerBackup.RegisterPacketHandler<P>(handler, obj);
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterPacketHandler{P}"/>
        public void TCP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterPacketHandler<P>(obj);
            else tcpPacketHandlerBackup.UnRegisterPacketHandler<P>(obj);
        }

        /// <inheritdoc cref="IPacketHandler.RegisterStaticPacketHandler{P}"/>
        public void UDP_RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterStaticPacketHandler<P>(handler);
            else udpPacketHandlerBackup.RegisterStaticPacketHandler<P>(handler);
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterStaticPacketHandler{P}"/>
        public void UDP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterStaticPacketHandler<P>();
            else udpPacketHandlerBackup.UnRegisterStaticPacketHandler<P>();
        }

        /// <inheritdoc cref="IPacketHandler.RegisterPacketHandler{P}"/>
        public void UDP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterPacketHandler<P>(handler, obj);
            else udpPacketHandlerBackup.RegisterPacketHandler<P>(handler, obj);
        }

        /// <inheritdoc cref="IPacketHandler.UnRegisterPacketHandler{P}"/>
        public void UDP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterPacketHandler<P>(obj);
            else udpPacketHandlerBackup.UnRegisterPacketHandler<P>(obj);
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

            // add pre-existing known types and the previous internal packet
            // delegate invoke structure. Hence, the new connection
            // is able to pick up the state of the previous TCP-Connection.
            KnownTypes.ForEach(TcpConnection.AddExternalPackets);
            tcpConnection.RestorePacketHandler(tcpPacketHandlerBackup);

            // add the internal ClientConnectionContainer close event.
            tcpConnection.ConnectionClosed += (closeReason, connection) =>
            {
                tcpPacketHandlerBackup = connection.BackupPacketHandler();
                connectionLost?.Invoke(tcpConnection, ConnectionType.TCP, closeReason);
                Reconnect();
            };

            connectionEstablished?.Invoke(tcpConnection, ConnectionType.TCP);
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

            // add pre-existing known types and the previous internal packet
            // delegate invoke structure. Hence, the new connection
            // is able to pick up the state of the previous UDP-Connection.
            KnownTypes.ForEach(UdpConnection.AddExternalPackets);
            udpConnection.RestorePacketHandler(udpPacketHandlerBackup);

            // add the internal ClientConnectionContainer close event.
            udpConnection.ConnectionClosed += (closeReason, connection) =>
            {
                udpPacketHandlerBackup = connection.BackupPacketHandler();
                connectionLost?.Invoke(this.udpConnection, ConnectionType.UDP, closeReason);
                Reconnect();
            };

            connectionEstablished?.Invoke(udpConnection, ConnectionType.UDP);
        }

        #endregion Opening New Connections

        #region Sending Packets

        /// <summary>
        /// Sends a ping over the TCP connection.
        /// </summary>
        public void SendPing()
        {
            if (tcpConnection != null && !tcpConnection.IsAlive)
            {
                if(ThrowExceptionOnUndeliverablePackets)
                    throw new ConnectionNotAliveException(tcpConnection, $"Can't send a ping, while the connection isn't alive.");
            }
            else tcpConnection.SendPing();
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> to the network via TCP. The sender will not receive an answer, due to
        /// no sender instance being given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void Send(Packet packet) => SendSlow(packet);

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
        public void Send(Packet packet, object instance) => SendSlow(packet, instance);

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
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket => await SendAsync<T>(packet, ConnectionType.TCP);

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
                throw new ArgumentException($"{nameof(ConnectionType)} '{type.ToString()}' isn't supported yet.");
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
                throw new ArgumentException($"{nameof(ConnectionType)} '{connectionType.ToString()}' isn't supported yet.");
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
            {
                if(ThrowExceptionOnUndeliverablePackets)
                    throw new ConnectionNotAliveException(tcpConnection, $"Can't send a {nameof(Packet)}, while the connection isn't alive.");
            }
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
            else if(ThrowExceptionOnUndeliverablePackets) throw new ConnectionNotAliveException(tcpConnection, $"Can't send a {nameof(Packet)}, while the connection isn't alive.");
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
            {
                if(ThrowExceptionOnUndeliverablePackets)
                    throw new ConnectionNotAliveException(tcpConnection, $"Can't send a {nameof(Packet)}, while the connection isn't alive.");
            }
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
            else if(ThrowExceptionOnUndeliverablePackets) throw new ConnectionNotAliveException(tcpConnection, $"Can't send a {nameof(Packet)}, while the connection isn't alive.");
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

        /// <inheritdoc />
        public void Dispose()
        {
            reconnectTimer.Elapsed -= TryToConnect;
            reconnectTimer.Stop();
            reconnectTimer.Dispose();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(ClientConnectionContainer)}. TCP is alive {IsAlive_TCP}. UDP is alive {IsAlive_UDP}. Server IPAddress {IPAddress} Port {Port.ToString()}";
        }
    }
}