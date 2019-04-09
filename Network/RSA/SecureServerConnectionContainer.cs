using System.Net.Sockets;

namespace Network.RSA
{
    /// <summary>
    /// A secure <see cref="ServerConnectionContainer"/>, implementing RSA encryption.
    /// </summary>
    /// <seealso cref="ServerConnectionContainer"/>
    public class SecureServerConnectionContainer : ServerConnectionContainer
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="ipAddress">The local ip address.</param>
        /// <param name="port">The local port.</param>
        /// <param name="rsaPair">The local RSA key-pair.</param>
        /// <param name="start">Whether to automatically starts to listen to tcp/udp/bluetooth clients.</param>
        internal SecureServerConnectionContainer(string ipAddress, int port, RSAPair rsaPair, bool start = true)
            : base(ipAddress, port, start)
        {
            RSAPair = rsaPair;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="port">The local port.</param>
        /// <param name="rsaPair">The local RSA key-pair.</param>
        /// <param name="start">Whether to automatically starts to listen to tcp/udp/bluetooth clients.</param>
        internal SecureServerConnectionContainer(int port, RSAPair rsaPair, bool start = true)
            : this(System.Net.IPAddress.Any.ToString(), port, rsaPair, start) { }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Creates and returns a <see cref="SecureTcpConnection"/> from the given <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to use for sending and receiving data.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        protected override TcpConnection CreateTcpConnection(TcpClient tcpClient) => ConnectionFactory.CreateSecureTcpConnection(tcpClient, RSAPair);

        #endregion Methods
    }
}