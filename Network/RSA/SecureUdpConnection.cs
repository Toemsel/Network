using Network.Converter;
using System;
using System.Net;
using System.Net.Sockets;
using Network.Packets;

namespace Network.RSA
{
    /// <summary>
    /// A secure <see cref="UdpConnection"/>, implementing RSA encryption.
    /// </summary>
    /// <seealso cref="UdpConnection"/>
    public class SecureUdpConnection : UdpConnection
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureUdpConnection"/> class.
        /// </summary>
        /// <param name="udpClient">The <see cref="UdpClient"/> to use for sending and receiving data.</param>
        /// <param name="remoteEndPoint">The remote end point to connect to.</param>
        /// <param name="rsaPair">The local RSA key-pair.</param>
        /// <param name="writeLock">Whether the connection has a write lock.</param>
        internal SecureUdpConnection(UdpClient udpClient, IPEndPoint remoteEndPoint, RSAPair rsaPair, bool writeLock = false)
            : base(udpClient, remoteEndPoint, writeLock, skipInitializationProcess: true)
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
    }
}