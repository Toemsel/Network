#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-24-2015
//
// Last Modified By : Thomas
// Last Modified On : 07-26-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2018
// </copyright>
// <License>
// GNU LESSER GENERAL PUBLIC LICENSE
// </License>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ***********************************************************************

#endregion Licence - LGPLv3

using Network.Enums;
using Network.Packets;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Network
{
    /// <summary>
    /// This class contains a tcp connection to the given tcp client.
    /// It provides convenient methods to send and receive objects with a minimal serialization header.
    /// </summary>
    public class TcpConnection : Connection
    {
        private TcpClient client;
        private NetworkStream stream;
        private Socket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpConnection"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client.</param>
        internal TcpConnection(TcpClient tcpClient, bool skipInitializationProcess = false)
            : base()
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

        /// <summary>
        /// Gets or sets the time to live for the tcp connection.
        /// </summary>
        /// <value>The TTL.</value>
        public override short TTL { get { return socket.Ttl; } set { socket.Ttl = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether [dual mode]. (Ipv6 + Ipv4)
        /// </summary>
        /// <value><c>true</c> if [dual mode]; otherwise, <c>false</c>.</value>
        public override bool DualMode { get { return socket.DualMode; } set { socket.DualMode = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TcpConnection"/> is allowed to fragment the frames.
        /// </summary>
        /// <value><c>true</c> if fragment; otherwise, <c>false</c>.</value>
        public override bool Fragment { get { return !socket.DontFragment; } set { socket.DontFragment = !value; } }

        /// <summary>
        /// The hop limit. This is compareable to the Ipv4 TTL.
        /// </summary>
        public override int HopLimit
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.HopLimit); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.HopLimit, value); }
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
            get { return (bool)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback, value); }
        }

        /// <summary>
        /// Gets or sets if the packet should be sent directly to its destination or not.
        /// </summary>
        public override bool IsRoutingEnabled
        {
            get { return !(bool)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.DontRoute); }
            set { socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.DontRoute, !value); }
        }

        /// <summary>
        /// Gets the local end point.
        /// </summary>
        /// <value>The local end point.</value>
        public EndPoint LocalEndPoint { get { return socket.LocalEndPoint; } }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        public EndPoint RemoteEndPoint { get { return socket.RemoteEndPoint; } }

        /// <summary>
        /// Establishes a udp connection.
        /// </summary>
        /// <returns>The EndPoint of the udp connection.</returns>
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

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
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

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void WriteBytes(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            if (ForceFlush) stream.Flush();
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
        /// This is not possible for the TCP connection, theoretically.
        /// </summary>
        protected override void HandleUnknownPacket()
        {
            Logger.Log($"Connection can't handle the received packet. No listener defined.", LogLevel.Error);
            CloseHandler(CloseReason.ReadPacketThreadException);
        }

        /// <summary>
        /// Gets the ip address's local endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public override IPEndPoint IPLocalEndPoint { get { return (IPEndPoint)client?.Client?.LocalEndPoint; } }

        /// <summary>
        /// Gets the ip address's remote endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public override IPEndPoint IPRemoteEndPoint { get { return (IPEndPoint)client?.Client?.RemoteEndPoint; } }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CloseSocket()
        {
            client.Close();
        }
    }
}