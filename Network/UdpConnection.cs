using Network.Enums;
using Network.Extensions;
using Network.Packets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    /// <summary>
    /// Builds upon the <see cref="Connection"/> class, implementing UDP and allowing for messages to be conveniently
    /// sent without a large serialisation header.
    /// </summary>
    public class UdpConnection : Connection
    {
        #region Variables

        /// <summary>
        /// The <see cref="UdpClient"/> for this <see cref="TcpConnection"/> instance.
        /// </summary>
        private readonly UdpClient client;

        /// <summary>
        /// The <see cref="Socket"/> for this <see cref="TcpConnection"/> instance.
        /// </summary>
        private readonly Socket socket;

        /// <summary>
        /// The local endpoint for the <see cref="client"/>.
        /// </summary>
        private IPEndPoint localEndPoint;

        /// <summary>
        /// The remote endpoint for the <see cref="client" />
        /// </summary>
        private IPEndPoint remoteEndPoint;

        /// <summary>
        /// Stopwatch to measure the RTT for ping packets.
        /// </summary>
        private readonly Stopwatch rttStopWatch = new Stopwatch();

        /// <summary>
        /// Cache of all received bytes.
        /// </summary>
        private readonly List<byte> receivedBytes = new List<byte>();

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpConnection"/> class.
        /// </summary>
        /// <param name="udpClient">The UDP client to use.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="writeLock">Whether the <see cref="UdpConnection"/> will have a write lock.</param>
        /// <param name="skipInitializationProcess">Whether to skip the call to <see cref="Connection.Init()"/>.</param>
        internal UdpConnection(UdpClient udpClient, IPEndPoint remoteEndPoint, bool writeLock = false, bool skipInitializationProcess = false)
            : base()
        {
            this.remoteEndPoint = remoteEndPoint;

            client = udpClient;
            AcknowledgePending = writeLock;
            socket = client.Client;
            localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;
            client.Connect(remoteEndPoint);
            ObjectMapRefreshed();

            KeepAlive = false;
            socket.SendTimeout = 0;
            socket.ReceiveTimeout = 0;

            if (IsWindows)
                socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);

            //The initialization has to be done elsewhere.
            //The caller of the constructor wants to apply
            //additional settings before starting the network comm.
            if (!skipInitializationProcess)
                Init();
        }

        #endregion Constructors

        #region Properties

        /// <inheritdoc />
        public override IPEndPoint IPLocalEndPoint { get { return localEndPoint; } }

        /// <summary>
        /// The local <see cref="EndPoint"/> for the <see cref="socket"/>.
        /// </summary>
        public EndPoint LocalEndPoint { get { return socket.LocalEndPoint; } }

        /// <inheritdoc />
        public override IPEndPoint IPRemoteEndPoint { get { return localEndPoint; } }

        /// <summary>
        /// The remote <see cref="EndPoint"/> for the <see cref="socket"/>.
        /// </summary>
        public EndPoint RemoteEndPoint { get { return remoteEndPoint; } }

        /// <inheritdoc />
        public override bool DualMode { get { return socket.DualMode; } set { socket.DualMode = value; } }

        /// <inheritdoc />
        public override bool Fragment { get { return !socket.DontFragment; } set { socket.DontFragment = !value; } }

        /// <inheritdoc />
        public override int HopLimit
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.HopLimit); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.HopLimit, value); }
        }

        /// <inheritdoc />
        public override bool IsRoutingEnabled
        {
            get { return !(bool)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.DontRoute); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.DontRoute, !value); }
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
            get { return (bool)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.UseLoopback); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.UseLoopback, value); }
        }

        /// <summary>
        /// Whether a checksum should be created for each UDP packet sent.
        /// </summary>
        public bool IsChecksumEnabled
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoChecksum) == 0; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoChecksum, value ? 0 : -1); }
        }

        /// <summary>
        /// Whether the connection has a write lock in place.
        /// </summary>
        internal bool AcknowledgePending { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Resets the <see cref="rttStopWatch"/> and sends a new <see cref="UDPPingRequest"/> packet, so that the RTT can
        /// be measured. The RTT will be placed in the <see cref="Connection.RTT"/> property.
        /// </summary>
        public void MeasureRTT()
        {
            rttStopWatch.Restart();
            Send(new UDPPingRequest(), this);
        }

        /// <summary>
        /// Handler for <see cref="UDPPingResponse"/> packets.
        /// </summary>
        /// <param name="response">The response packet received.</param>
        /// <param name="connection">The connection that sent the response.</param>
        internal void UDPPingResponse(UDPPingResponse response, Connection connection)
        {
            rttStopWatch.Stop();
            RTT = rttStopWatch.ElapsedMilliseconds;
            Ping = RTT / 2;
        }

        /// <inheritdoc />
        public override void ObjectMapRefreshed()
        {
            //Register again the UDP default requests and responses.
            RegisterPacketHandler<UDPPingRequest>((u, c) => Send(new UDPPingResponse(u), this), this);
            RegisterPacketHandler<UDPPingResponse>(UDPPingResponse, this);
            base.ObjectMapRefreshed();
        }

        /// <inheritdoc />
        protected override byte[] ReadBytes(int amount)
        {
            while (AcknowledgePending && IsAlive)
                Thread.Sleep(IntPerformance);

            if (amount == 0) return new byte[0];
            while (receivedBytes.Count < amount)
            {
                receivedBytes.AddRange(client.Receive(ref localEndPoint).GetEnumerator().ToList<byte>());
                Thread.Sleep(IntPerformance);
            }

            byte[] data = new byte[amount];
            receivedBytes.CopyTo(0, data, 0, data.Length);
            receivedBytes.RemoveRange(0, data.Length);
            return data;
        }

        /// <inheritdoc />
        protected override void WriteBytes(byte[] bytes)
        {
            while (AcknowledgePending && IsAlive)
                Thread.Sleep(IntPerformance);

            client.Send(bytes, bytes.Length);
        }

        /// <inheritdoc />
        protected override void HandleUnknownPacket()
        {
            Logger.Log($"Connection can't handle the received packet. No listener defined.", LogLevel.Warning);
            //Ignore an unkown packet, we could have lost the AddPacketTypeRequest.
        }

        /// <inheritdoc />
        protected override void CloseHandler(CloseReason closeReason)
        {
            Close(closeReason, true);
        }

        /// <inheritdoc />
        protected override void CloseSocket()
        {
            socket.Close();
            client.Close();
        }

        #endregion Methods
    }
}