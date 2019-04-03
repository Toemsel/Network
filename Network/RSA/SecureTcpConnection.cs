using Network.Converter;
using System;
using System.Net;
using System.Net.Sockets;

namespace Network.RSA
{
    /// <summary>
    /// This class contains a tcp connection to the given tcp client.
    /// It provides convenient methods to send and receive objects with a minimal serialization header.
    /// Compared to the <see cref="TcpConnection"/> the <see cref="SecureTcpConnection"/> does encrypt/decrypt sent/received bytes.
    /// </summary>
    public class SecureTcpConnection : TcpConnection
    {
        #region Constructors

        internal SecureTcpConnection(RSAPair rsaPair, TcpClient tcpClient)
            : base(tcpClient, skipInitializationProcess: true)
        {
            //Setup the RSAConnectionHelper object.
            RSAConnection = new RSAConnection(this, rsaPair);
            PacketConverter = base.PacketConverter;
            base.PacketConverter = RSAConnection;

            //Since we did skip the initialization,... DO IT!
            Init();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The PublicKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PublicKey => RSAConnection.RSAPair.Public;

        /// <summary>
        /// The PrivateKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PrivateKey => RSAConnection.RSAPair.Private;

        /// <summary>
        /// The used KeySize of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public int KeySize => RSAConnection.RSAPair.KeySize;

        /// <summary>
        /// Gets the RSA pair.
        /// </summary>
        /// <value>The RSA pair.</value>
        public RSAPair RSAPair => RSAConnection.RSAPair;

        /// <summary>
        /// Use your own packetConverter to serialize/deserialze objects.
        /// Take care that the internal packet structure should still remain the same:
        ///     1. [16bits]  packet type
        ///     2. [32bits]  packet length
        ///     3. [xxbits]  packet data
        /// The default packetConverter uses reflection to get and set data within objects.
        /// Using your own packetConverter could result in a higher throughput.
        /// </summary>
        public override IPacketConverter PacketConverter
        {
            get => RSAConnection.PacketConverter;
            set => RSAConnection.PacketConverter = value;
        }

        /// <summary>
        /// A helper object to handle RSA requests.
        /// </summary>
        /// <value>The RSA connection.</value>
        private RSAConnection RSAConnection { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Instead of a normal UdpConnection, we create a secure-UdpConnection
        /// based on the configuration of our secure-TcpConnection. (Sharing private/public key)
        /// </summary>
        /// <param name="localEndPoint">The localEndPoint.</param>
        /// <param name="removeEndPoint">The removeEndPoint to connect to.</param>
        /// <param name="writeLock">The writeLock.</param>
        /// <returns>A Secure-UdpConnection.</returns>
        protected override UdpConnection CreateUdpConnection(IPEndPoint localEndPoint, IPEndPoint removeEndPoint, bool writeLock) =>
            new SecureUdpConnection(new UdpClient(localEndPoint), removeEndPoint, RSAPair, writeLock);

        #endregion Methods
    }
}