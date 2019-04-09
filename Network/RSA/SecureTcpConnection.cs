using Network.Converter;
using System;
using System.Net;
using System.Net.Sockets;
using Network.Packets;

namespace Network.RSA
{
    /// <summary>
    /// A secure <see cref="TcpConnection"/>, implementing RSA encryption.
    /// </summary>
    /// <seealso cref="TcpConnection"/>
    public class SecureTcpConnection : TcpConnection
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureTcpConnection"/> class.
        /// </summary>
        /// <param name="rsaPair">The local RSA key-pair.</param>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to use for sending and receiving data.</param>
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
        /// The public RSA key.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PublicKey => RSAConnection.RSAPair.Public;

        /// <summary>
        /// The private RSA key.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PrivateKey => RSAConnection.RSAPair.Private;

        /// <summary>
        /// The size of the RSA keys.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public int KeySize => RSAConnection.RSAPair.KeySize;

        /// <summary>
        /// The RSA key-pair used for encryption.
        /// </summary>
        public RSAPair RSAPair => RSAConnection.RSAPair;

        /// <summary>
        /// Allows the usage of a custom <see cref="IPacketConverter"/> implementation for serialisation and deserialisation.
        /// However, the internal structure of the packet should stay the same:
        ///     Packet Type     : 2  bytes (ushort)
        ///     Packet Length   : 4  bytes (int)
        ///     Packet Data     : xx bytes (actual serialised packet data)
        /// </summary>
        /// <remarks>
        /// The default <see cref="PacketConverter"/> uses reflection (with type property caching) for serialisation
        /// and deserialisation. This allows good performance over the widest range of packets. Should you want to
        /// handle only a specific set of packets, a custom <see cref="IPacketConverter"/> can allow more throughput (no slowdowns
        /// due to relatively slow reflection).
        /// </remarks>
        public override IPacketConverter PacketConverter
        {
            get => RSAConnection.PacketConverter;
            set => RSAConnection.PacketConverter = value;
        }

        /// <summary>
        /// A <see cref="Connection"/> to send and receive <see cref="Packet"/> objects that supports RSA encryption.
        /// </summary>
        private RSAConnection RSAConnection { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Creates a <see cref="SecureUdpConnection"/> that implements RSA encryption.
        /// </summary>
        /// <param name="localEndPoint">The local end point.</param>
        /// <param name="removeEndPoint">The remote end point.</param>
        /// <param name="writeLock">Whether the <see cref="SecureUdpConnection"/> has a write lock.</param>
        /// <returns>The created <see cref="SecureUdpConnection"/>.</returns>
        protected override UdpConnection CreateUdpConnection(IPEndPoint localEndPoint, IPEndPoint removeEndPoint, bool writeLock) =>
            new SecureUdpConnection(new UdpClient(localEndPoint), removeEndPoint, RSAPair, writeLock);

        #endregion Methods
    }
}