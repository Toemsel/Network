using Network.Attributes;

namespace Network.Packets.RSA
{
    /// <summary>
    /// Response packet for a <see cref="RSAKeyInformationRequest"/>.
    /// </summary>
    [PacketType(12), PacketRequest(typeof(RSAKeyInformationRequest))]
    internal class RSAKeyInformationResponse : ResponsePacket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAKeyInformationResponse"/> class.
        /// </summary>
        /// <param name="publicKey">The public key to share.</param>
        /// <param name="keySize">The size of the public key.</param>
        /// <param name="request">The handled <see cref="RSAKeyInformationRequest"/>.</param>
        internal RSAKeyInformationResponse(string publicKey, int keySize, RSAKeyInformationRequest request) : base(request)
        {
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

        #endregion Properties
    }
}