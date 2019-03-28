using System;
using System.Threading.Tasks;

namespace Network.RSA
{
    public class SecureClientConnectionContainer : ClientConnectionContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        internal SecureClientConnectionContainer(string ipAddress, int port, RSAPair rsaPair)
            : base(ipAddress, port)
        {
            RSAPair = rsaPair;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <param name="udpConnection">The UDP connection.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        internal SecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection, RSAPair rsaPair)
            : base(tcpConnection.IPRemoteEndPoint.Address.ToString(), tcpConnection.IPRemoteEndPoint.Port)
        {
            RSAPair = rsaPair;
        }

        /// <summary>
        /// Creates a new SecureTcpConnection.
        /// </summary>
        /// <returns>A TcpConnection.</returns>
        protected override async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnection() => await ConnectionFactory.CreateSecureTcpConnectionAsync(IPAddress, Port, RSAPair);

        /// <summary>
        /// Creates a new SecureUdpConnection from the existing SecureTcpConnection.
        /// </summary>
        /// <returns>A UdpConnection.</returns>
        protected override async Task<Tuple<UdpConnection, ConnectionResult>> CreateUdpConnection() => await ConnectionFactory.CreateSecureUdpConnectionAsync(TcpConnection, RSAPair);
    }
}