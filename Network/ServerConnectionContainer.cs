using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Network.Enums;
using Network.Packets;

namespace Network
{
    /// <summary>
    /// Provides convenient methods to reduce the number of code lines which are needed to manage all connected clients.
    /// </summary>
    public class ServerConnectionContainer : ConnectionContainer
    {
        #region Variables

        /// <summary>
        /// Listens for TCP clients.
        /// </summary>
        /// <remarks>
        /// UDP clients are accepted via TCP, as they require an existing TCP connection before they are accepted.
        /// </remarks>
        private TcpListener tcpListener;

        // TODO Remove all occurrences of backing fields for events in favor of new, cleaner 'event?.Invoke(args)' syntax

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<Connection, ConnectionType, CloseReason> connectionLost;

        /// <summary>
        /// A handler which will be invoked if a new connection is established.
        /// </summary>
        private event Action<Connection, ConnectionType> connectionEstablished;

        /// <summary>
        /// Maps all <see cref="TcpConnection"/>s currently connected to the server to any <see cref="UdpConnection"/>s
        /// they may own.
        /// </summary>
        private readonly ConcurrentDictionary<TcpConnection, List<UdpConnection>> connections = new ConcurrentDictionary<TcpConnection, List<UdpConnection>>();

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="ipAddress">The local ip address.</param>
        /// <param name="port">The local port.</param>
        /// <param name="start">Whether to automatically start listening for clients after instantiation.</param>
        internal ServerConnectionContainer(string ipAddress, int port, bool start = true)
            : base(ipAddress, port)
        {
            if (start)
                _ = Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="port">The local port.</param>
        /// <param name="start">Whether to automatically start listening for clients after instantiation.</param>
        internal ServerConnectionContainer(int port, bool start = true)
            : this(System.Net.IPAddress.IPv6Any.ToString(), port, start) { }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Whether the TCP server is currently online.
        /// </summary>
        public bool IsTCPOnline { get; private set; } = false;

        /// <summary>
        /// Whether <see cref="UdpConnection"/>s are allowed to connect.
        /// If <c>False</c> the client's can't use the <see cref="ClientConnectionContainer" />,
        /// since the <see cref="ClientConnectionContainer" /> automatically tries to establish a <see cref="UdpConnection" />.
        /// When a client requests a <see cref="UdpConnection" /> while <see cref="AllowUDPConnections"/> is set to false,
        /// the client's <see cref="TcpConnection" /> will be killed automatically, due to an inappropriate request.
        /// </summary>
        public bool AllowUDPConnections { get; set; } = true;

        /// <summary>
        /// The maximum amount of <see cref="UdpConnection"/>s that a single <see cref="TcpConnection"/> can own.
        /// </summary>
        /// <remarks>
        /// When a <see cref="ClientConnectionContainer"/> requests a <see cref="UdpConnection"/> once they have already
        /// reached this limit, all existing connections (both <see cref="TcpConnection"/>s and <see cref="UdpConnection"/>s)
        /// will be closed.
        /// </remarks>
        public int UDPConnectionLimit { get; set; } = 1;

        /// <summary>
        /// Lists all currently connected <see cref="TcpConnection"/>s.
        /// </summary>
        public List<TcpConnection> TCP_Connections { get { return connections.Keys.ToList(); } }

        /// <summary>
        /// Lists all currently connected <see cref="UdpConnection"/>s.
        /// </summary>
        public List<UdpConnection> UDP_Connections { get { return connections.Values.SelectMany(c => c).ToList(); } }

        /// <summary>
        /// The amount of currently connected clients. Includes TCP and UDP clients.
        /// </summary>
        public int Count { get { return connections.Count; } }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Starts both a Bluetooth and TCP server, and listens for incoming connections.
        /// </summary>
        public async Task Start() => await StartTCPListener();

        /// <summary>
        /// Starts a TCP server and listens for incoming <see cref="TcpConnection"/>s.
        /// </summary>
        public async Task StartTCPListener()
        {
            if (IsTCPOnline) return;

            tcpListener = new TcpListener(System.Net.IPAddress.Parse(IPAddress), Port);
            tcpListener.Server.DualMode = true;
            IsTCPOnline = !IsTCPOnline;
            tcpListener.Start();

            try
            {
                while (IsTCPOnline)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    TcpConnection tcpConnection = CreateTcpConnection(tcpClient);
                    tcpConnection.NetworkConnectionClosed += connectionClosed;
                    tcpConnection.ConnectionEstablished += udpConnectionReceived;
                    connections.GetOrAdd(tcpConnection, new List<UdpConnection>());

                    //Inform all subscribers.
                    if (connectionEstablished != null &&
                        connectionEstablished.GetInvocationList().Length > 0)
                        connectionEstablished(tcpConnection, ConnectionType.TCP);

                    KnownTypes.ForEach(tcpConnection.AddExternalPackets);
                }
            }
            //The TCP-Listener has been shut down.
            catch(ObjectDisposedException) { }
            catch(SocketException) { }
        }

        /// <summary>
        /// Handles when a <see cref="UdpConnection"/> successfully connects to the server.
        /// </summary>
        /// <param name="tcpConnection">The parent <see cref="TcpConnection"/>.</param>
        /// <param name="udpConnection">The connected <see cref="UdpConnection"/>.</param>
        private void udpConnectionReceived(TcpConnection tcpConnection, UdpConnection udpConnection)
        {
            if (!AllowUDPConnections || this[tcpConnection].Count >= UDPConnectionLimit)
            {
                CloseReason closeReason = (this[tcpConnection].Count >= UDPConnectionLimit) ? CloseReason.UdpLimitExceeded : CloseReason.InvalidUdpRequest;
                tcpConnection.Close(closeReason, true);
                return;
            }

            this[tcpConnection].Add(udpConnection);
            udpConnection.NetworkConnectionClosed += connectionClosed;
            KnownTypes.ForEach(udpConnection.AddExternalPackets);

            //Inform all subscribers.
            if (connectionEstablished != null &&
                connectionEstablished.GetInvocationList().Length > 0)
                connectionEstablished(udpConnection, ConnectionType.UDP);
        }

        /// <summary>
        /// Handles a connection closure.
        /// </summary>
        /// <param name="closeReason">The reason for the <see cref="Connection"/> being closed.</param>
        /// <param name="connection">The <see cref="Connection"/> that closed.</param>
        private void connectionClosed(CloseReason closeReason, Connection connection)
        {
            if (connection.GetType().Equals(typeof(TcpConnection)))
            {
                List<UdpConnection> udpConnections = new List<UdpConnection>();
                TcpConnection tcpConnection = (TcpConnection)connection;
                while (!connections.TryRemove(tcpConnection, out udpConnections))
                    Thread.Sleep(new Random().Next(0, 8)); //If we could not remove the tcpConnection, try it again.
                udpConnections.ForEach(u => u.ExternalClose(closeReason));

                // cleanup the event handler for the TCP connection.
                connection.ConnectionEstablished -= udpConnectionReceived;
            }
            else if (connection.GetType().Equals(typeof(UdpConnection)))
            {
                TcpConnection tcpConnection = this[(UdpConnection)connection];
                //UDP connection already removed because the TCP connection is already dead.
                if (tcpConnection == null) return;
                connections[tcpConnection].Remove((UdpConnection)connection);
            }

            if (connectionLost != null &&
                connectionLost.GetInvocationList().Length > 0 &&
                connection.GetType().Equals(typeof(TcpConnection)))
                connectionLost(connection, ConnectionType.TCP, closeReason);
            else if (connectionLost != null &&
                connectionLost.GetInvocationList().Length > 0 &&
                connection.GetType().Equals(typeof(UdpConnection)))
                connectionLost(connection, ConnectionType.UDP, closeReason);

            // remove the connection lost event handler to enable GC.
            connection.NetworkConnectionClosed -= connectionClosed;
        }

        /// <summary>
        /// Stops the TCP listener, so that no new <see cref="TcpConnection"/>s can connect.
        /// </summary>
        public void StopTCPListener()
        {
            if (IsTCPOnline) tcpListener.Stop();
            IsTCPOnline = !IsTCPOnline;
        }

        /// <summary>
        /// Stops both the Bluetooth and TCP listeners, so that no new connections can connect.
        /// </summary>
        public void Stop() => StopTCPListener();

        /// <summary>
        /// Closes all currently connected <see cref="Connection"/>s (be it Bluetooth, TCP, or UDP).
        /// </summary>
        /// <param name="reason">The reason for the connection closure.</param>
        public void CloseConnections(CloseReason reason)
        {
            CloseTCPConnections(reason);
            CloseUDPConnections(reason);
        }

        /// <summary>
        /// Closes all currently connected <see cref="TcpConnection"/>s.
        /// </summary>
        /// <param name="reason">The reason for the connection closure.</param>
        public void CloseTCPConnections(CloseReason reason)
        {
            connections.Keys.ToList().ForEach(c => c.Close(reason));
        }

        /// <summary>
        /// Closes all currently connected <see cref="UdpConnection"/>s.
        /// </summary>
        /// <param name="reason">The reason for the connection closure.</param>
        public void CloseUDPConnections(CloseReason reason)
        {
            connections.Values.ToList().ForEach(c => c.ForEach(b => b.Close(reason)));
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> to all currently connected <see cref="TcpConnection"/>s.
        /// </summary>
        /// <param name="packet">The packet to send via broadcast.</param>
        public void TCP_BroadCast(Packet packet)
        {
            connections.Keys.ToList().ForEach(c => c.Send(packet));
        }

        /// <summary>
        /// Sends the given <see cref="Packet"/> to all currently connected <see cref="UdpConnection"/>s.
        /// </summary>
        /// <param name="packet">The packet to send via broadcast.</param>
        public void UDP_BroadCast(Packet packet) => connections.Values.ToList().ForEach(c => c.ForEach(b => b.Send(packet)));

        /// <summary>
        /// Creates a new <see cref="TcpConnection"/> instance from the given <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to use for the <see cref="TcpConnection"/>.</param>
        /// <returns>A <see cref="TcpConnection"/> that uses the given <see cref="TcpClient"/> to send data to and from the client.</returns>
        protected virtual TcpConnection CreateTcpConnection(TcpClient tcpClient) => ConnectionFactory.CreateTcpConnection(tcpClient);

        /// <inheritdoc />
        public override string ToString() =>
            $"ServerConnectionContainer. IsOnline {IsTCPOnline}. " +
            $"EnableUDPConnection {AllowUDPConnections}. " +
            $"UDPConnectionLimit {UDPConnectionLimit}. " +
            $"Connected TCP connections {connections.Count}.";

        #endregion Methods

        #region Indexers

        /// <summary>
        /// Returns all <see cref="UdpConnection"/>s that exist for the given <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> whose child <see cref="UdpConnection"/>s to return.</param>
        /// <returns>
        /// A <see cref="List{UdpConnection}"/> holding all child UDP connections of the given <see cref="TcpConnection"/>.
        /// </returns>
        public List<UdpConnection> this[TcpConnection tcpConnection]
        {
            get
            {
                if (connections.ContainsKey(tcpConnection))
                    return connections[tcpConnection];
                return null;
            }
        }

        /// <summary>
        /// Returns the parent <see cref="TcpConnection"/> of the given <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="udpConnection">The <see cref="UdpConnection"/> whose parent <see cref="TcpConnection"/> to return.</param>
        /// <returns>The <see cref="TcpConnection"/> which owns the given <see cref="UdpConnection"/>.</returns>
        public TcpConnection this[UdpConnection udpConnection]
        {
            get { return connections.SingleOrDefault(c => c.Value.Count(uc => uc.GetHashCode().Equals(udpConnection.GetHashCode())) > 0).Key; }
        }

        #endregion Indexers

        #region Events

        /// <summary>
        /// Occurs when [connection closed]. This action will be called if a TCP or an UDP has been closed.
        /// If a TCP connection has been closed, all its attached UDP connections are lost as well.
        /// If a UDP connection has been closed, the attached TCP connection may still be alive.
        /// </summary>
        public event Action<Connection, ConnectionType, CloseReason> ConnectionLost
        {
            add { connectionLost += value; }
            remove { connectionLost -= value; }
        }

        /// <summary>
        /// Signifies that a new <see cref="Connection"/> (i.e. <see cref="TcpConnection"/> or <see cref="UdpConnection"/>)
        /// has connected successfully to the server.
        /// </summary>
        public event Action<Connection, ConnectionType> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

        #endregion Events
    }
}