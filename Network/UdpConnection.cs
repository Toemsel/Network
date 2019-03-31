﻿using Network.Enums;
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
    /// This class contains a udp connection to the given udp client.
    /// It provides convenient methods to send and receive objects with a minimal serialization header.
    /// </summary>
    public class UdpConnection : Connection
    {
        private Socket socket;
        private UdpClient client;
        private IPEndPoint localEndPoint;

        /// <summary>
        /// Stopwatch to measure the RTT.
        /// </summary>
        private Stopwatch rttStopWatch = new Stopwatch();

        /// <summary>
        /// The received data cache.
        /// </summary>
        private List<byte> receivedBytes = new List<byte>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpConnection"/> class.
        /// </summary>
        /// <param name="udpClient">The UDP client.</param>
        /// <param name="endPoint">The endPoint where we want to receive the data.</param>
        internal UdpConnection(UdpClient udpClient, IPEndPoint remoteEndPoint, bool writeLock = false, bool skipInitializationProcess = false)
            : base()
        {
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
                Initialise();
        }

        /// <summary>
        /// Gets or sets the time to live for the tcp connection.
        /// </summary>
        /// <value>The TTL.</value>
        public override short TTL { get { return client.Ttl; } set { client.Ttl = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether [dual mode]. (Ipv6 + Ipv4)
        /// </summary>
        /// <value><c>true</c> if [dual mode]; otherwise, <c>false</c>.</value>
        public override bool DualMode { get { return socket.DualMode; } set { socket.DualMode = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UdpConnection"/> is allowed to fragment the frames.
        /// </summary>
        /// <value><c>true</c> if fragment; otherwise, <c>false</c>.</value>
        public override bool Fragment { get { return !socket.DontFragment; } set { socket.DontFragment = !value; } }

        /// <summary>
        /// Gets or sets if a UDP packet checksum should be created.
        /// </summary>
        public bool IsChecksumEnabled
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoChecksum) == 0; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoChecksum, value ? 0 : -1); }
        }

        /// <summary>
        /// The hop limit. This is compareable to the Ipv4 TTL.
        /// </summary>
        public override int HopLimit
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.HopLimit); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.HopLimit, value); }
        }

        /// <summary>
        /// Gets or sets if the packet should be send with or without any delay.
        /// If disabled, no data will be buffered at all and sent immediately to it's destination.
        /// There is no guarantee that the network performance will be increased.
        /// </summary>
        public override bool NoDelay
        {
            get { return client.Client.NoDelay; }
            set { client.Client.NoDelay = value; }
        }

        /// <summary>
        /// Gets or sets if it should bypass hardware.
        /// </summary>
        public override bool UseLoopback
        {
            get { return (bool)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.UseLoopback); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.UseLoopback, value); }
        }

        /// <summary>
        /// Gets or sets if the packet should be sent directly to its destination or not.
        /// </summary>
        public override bool IsRoutingEnabled
        {
            get { return !(bool)socket.GetSocketOption(SocketOptionLevel.Udp, SocketOptionName.DontRoute); }
            set { socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.DontRoute, !value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [write lock].
        /// </summary>
        /// <value><c>true</c> if [write lock]; otherwise, <c>false</c>.</value>
        internal bool AcknowledgePending { get; set; }

        /// <summary>
        /// Measures the RTT of the UDP connection.
        /// Receiving a result
        /// </summary>
        /// <param name="rttResult">The RTT result.</param>
        public void MeasureRTT()
        {
            rttStopWatch.Restart();
            Send(new UDPPingRequest(), this);
        }

        /// <summary>
        /// We received a UDP RTT request response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="connection">The connection.</param>
        internal void UDPPingResponse(UDPPingResponse response, Connection connection)
        {
            rttStopWatch.Stop();
            RTT = rttStopWatch.ElapsedMilliseconds;
            Ping = RTT / 2;
        }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override byte[] ReadBytes(int amount)
        {
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

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void WriteBytes(byte[] bytes)
        {
            while (AcknowledgePending && IsAlive)
                Thread.Sleep(IntPerformance);
            client.Send(bytes, bytes.Length);
        }

        /// <summary>
        /// Handles if the connection should be closed, based on the reason.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        protected override void CloseHandler(CloseReason closeReason)
        {
            Close(closeReason, true);
        }

        /// <summary>
        /// Handles the case if we receive an unknown packet.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void HandleUnknownPacket()
        {
            Logger.Log($"Connection can't handle the received packet. No listener defined.", LogLevel.Warning);
            //Ignore an unkown packet, we could have lost the AddPacketTypeRequest.
        }

        /// <summary>
        /// The packetHandlerMap has been refreshed.
        /// </summary>
        public override void ObjectMapRefreshed()
        {
            //Register again the UDP default requests and responses.
            RegisterPacketHandler<UDPPingRequest>((u, c) => Send(new UDPPingResponse(u), this), this);
            RegisterPacketHandler<UDPPingResponse>(UDPPingResponse, this);
            base.ObjectMapRefreshed();
        }

        /// <summary>
        /// Gets the ip address's local endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public override IPEndPoint LocalIPEndPoint { get { return (IPEndPoint)client?.Client?.LocalEndPoint; } }

        /// <summary>
        /// Gets the ip address's remote endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public override IPEndPoint RemoteIPEndPoint { get { return (IPEndPoint)client?.Client?.RemoteEndPoint; } }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CloseSocket()
        {
            socket.Close();
            client.Close();
        }
    }
}