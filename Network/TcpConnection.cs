using Network.Enums;
using Network.Packets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    /// <summary>
    /// Builds upon the <see cref="Connection"/> class, implementing TCP and allowing for messages to be conveniently
    /// sent without a large serialisation header.
    /// </summary>
    public class TcpConnection : Connection
    {
        #region Variables

        /// <summary>
        /// The <see cref="TcpClient"/> for this <see cref="TcpConnection"/> instance.
        /// </summary>
        private readonly TcpClient client;

        /// <summary>
        /// The <see cref="NetworkStream"/> on which to send and receive data.
        /// </summary>
        private readonly NetworkStream stream;

        /// <summary>
        /// The <see cref="Socket"/> for this <see cref="TcpConnection"/> instance.
        /// </summary>
        private readonly Socket socket;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnection"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client to use.</param>
        /// <param name="skipInitializationProcess">
        /// Whether to skip the initialisation process for the underlying <see cref="Connection"/>. If <c>true</c>
        /// <see cref="Connection.Init()"/> will have to be manually called later.
        /// </param>
        internal TcpConnection(TcpClient tcpClient, bool skipInitializationProcess = false) : base()
        {
            client = tcpClient;
            socket = tcpClient.Client;
            stream = client.GetStream();

            KeepAlive = true;
            ForceFlush = true;
            tcpClient.NoDelay = true;
            tcpClient.SendTimeout = 0;
            tcpClient.ReceiveTimeout = 0;
            tcpClient.LingerState = new LingerOption(true, TIMEOUT);

            //The initialization has to be done elsewhere.
            //The caller of the constructor wants to apply
            //additional settings before starting the network comm.
            if (!skipInitializationProcess)
                Init();
        }

        #endregion Constructors

        #region Properties

        /// <inheritdoc />
        public override IPEndPoint IPLocalEndPoint { get { return (IPEndPoint)client?.Client?.LocalEndPoint; } }

        /// <summary>
        /// The local <see cref="EndPoint"/> for the <see cref="socket"/>.
        /// </summary>
        public EndPoint LocalEndPoint { get { return socket.LocalEndPoint; } }

        /// <inheritdoc />
        public override IPEndPoint IPRemoteEndPoint { get { return (IPEndPoint)client?.Client?.RemoteEndPoint; } }

        /// <summary>
        /// The remote <see cref="EndPoint"/> for the <see cref="socket"/>.
        /// </summary>
        public EndPoint RemoteEndPoint { get { return socket.RemoteEndPoint; } }

        /// <inheritdoc />
        public override bool DualMode { get { return socket.DualMode; } set { socket.DualMode = value; } }

        /// <inheritdoc />
        public override bool Fragment { get { return !socket.DontFragment; } set { socket.DontFragment = !value; } }

        /// <inheritdoc />
        public override int HopLimit
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.HopLimit); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.HopLimit, value); }
        }

        /// <inheritdoc />
        public override bool IsRoutingEnabled
        {
            get { return !(bool)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.DontRoute); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.DontRoute, !value); }
        }

        /// <inheritdoc />
        public override bool NoDelay
        {
            get { return client.Client.NoDelay; }
            set { client.Client.NoDelay = value; }
        }

        /// <inheritdoc />
        public override short TTL { get { return socket.Ttl; } set { socket.Ttl = value; } }

        /// <inheritdoc />
        public override bool UseLoopback
        {
            get { return (bool)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback, value); }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Establishes a <see cref="UdpConnection"/> with the remote endpoint.
        /// </summary>
        /// <param name="connectionEstablished">The action to perform upon connection.</param>
        internal void EstablishUdpConnection(Action<IPEndPoint, IPEndPoint> connectionEstablished)
        {
            IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, GetFreePort());
            RegisterPacketHandler<EstablishUdpResponse>((packet, connection) =>
            {
                UnRegisterPacketHandler<EstablishUdpResponse>(this);
                connectionEstablished.Invoke(udpEndPoint, new IPEndPoint(IPRemoteEndPoint.Address, packet.UdpPort));
                Send(new EstablishUdpResponseACK());
            }, this);

            Send(new EstablishUdpRequest(udpEndPoint.Port), this);
        }

        /// <inheritdoc />
        protected override byte[] ReadBytes(int amount)
        {
            if (amount == 0) return new byte[0];
            byte[] requestedBytes = new byte[amount];
            int receivedIndex = 0;

            while (receivedIndex < amount)
            {
                while (client.Available == 0)
                    Thread.Sleep(IntPerformance);

                int readAmount = (amount - receivedIndex >= client.Available) ? client.Available : amount - receivedIndex;
                stream.Read(requestedBytes, receivedIndex, readAmount);
                receivedIndex += readAmount;
            }

            return requestedBytes;
        }

        /// <inheritdoc />
        protected override void WriteBytes(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            if (ForceFlush) stream.Flush();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Since TCP ensures the ordering of packets, we will always receive the <see cref="AddPacketTypeRequest"/> before
        /// a <see cref="Packet"/> of the unknown type. Thus, it is theoretically impossible that this method is called for
        /// a <see cref="TcpConnection"/> instance. Still gotta handle it though :),
        /// </remarks>
        protected override void HandleUnknownPacket()
        {
            Logger.Log("Connection can't handle the received packet. No listener defined.", LogLevel.Error);
            CloseHandler(CloseReason.ReadPacketThreadException);
        }

        /// <inheritdoc />
        protected override void CloseHandler(CloseReason closeReason) => Close(closeReason, true);

        /// <inheritdoc />
        protected override void CloseSocket() => client.Close();

        #endregion Methods
    }
}