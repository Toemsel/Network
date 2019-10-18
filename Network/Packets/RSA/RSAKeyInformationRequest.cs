using Network.Attributes;

namespace Network.Packets.RSA
{
    /// <summary>
    /// Requests a RSA public key from the paired <see cref="Connection"/>.
    /// </summary>
    /// <seealso cref="Packet" />
    [PacketType(11)]
    internal class RSAKeyInformationRequest : RequestPacket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAKeyInformationRequest"/> class.
        /// </summary>
        /// <param name="publicKey">The public key to share.</param>
        /// <param name="keySize">The size of the public key.</param>
        /// <param name="enableOAEPadding">Use OAE Padding.</param>
        internal RSAKeyInformationRequest(string publicKey, int keySize, bool enableOAEPadding)
        {
            UseOAEPadding = enableOAEPadding;
            PublicKey = publicKey;
            KeySize = keySize;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The public RSA key for encryption, decryption and signing.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// The size of the RSA key.
        /// </summary>
        public int KeySize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the use OAE Padding.
        /// </summary>
        /// <value><c>true</c> if [use oae padding]; otherwise, <c>false</c>.</value>
        public bool UseOAEPadding { get; set; }

        #endregion Properties
    }
}