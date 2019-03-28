using System.Net.Sockets;

namespace Network.RSA
{
    /// <summary>
    /// Is able to open and close connections to clients in a secure way.
    /// Handles basic client connection requests and provides useful methods
    /// to manage the existing connection.
    /// </summary>
    public class SecureServerConnectionContainer : ServerConnectionContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to tcp/udp/bluetooth clients.</param>
        internal SecureServerConnectionContainer(string ipAddress, int port, RSAPair rsaPair, bool start = true)
            : base(ipAddress, port, start)
        {
            RSAPair = rsaPair;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        internal SecureServerConnectionContainer(int port, RSAPair rsaPair, bool start = true)
            : this(System.Net.IPAddress.Any.ToString(), port, rsaPair, start) { }

        /// <summary>
        /// Instead of a normal TcpConnection, a secure server connection demands a secureTcpConnection.
        /// </summary>
        /// <param name="tcpClient">The tcpClient to be wrapped.</param>
        /// <returns>A <see cref="SecureTcpConnection"/></returns>
        protected override TcpConnection CreateTcpConnection(TcpClient tcpClient) => ConnectionFactory.CreateSecureTcpConnection(tcpClient, RSAPair);
    }
}