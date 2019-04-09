namespace Network.RSA
{
    /// <summary>
    /// Stores an RSA public/private key-pair.
    /// </summary>
    public class RSAPair
    {
        #region Variables

        /// <summary>
        /// Replacement value to be used if the private key is unknown, i.e. when receiving an <see cref="RSAPair"/> from
        /// a remote <see cref="RSAConnection"/>.
        /// </summary>
        private const string PRIVATE_KEY_UNKNOWN = "UNKNOWN";

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class.
        /// Generate a <see cref="RSAPair" /> with <see cref="RSAKeyGeneration.Generate(int)"/>.
        /// </summary>
        /// <param name="publicKey">The public key (https://superdry.apphb.com/tools/online-rsa-key-converter).</param>
        /// <param name="privateKey">The private key (https://superdry.apphb.com/tools/online-rsa-key-converter).</param>
        /// <param name="keySize">The size of the key.</param>
        public RSAPair(string publicKey, string privateKey, int keySize)
        {
            Private = privateKey;
            Public = publicKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class. Used for internal purposes. The communication
        /// partner only sends us his public key to encrypt.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <param name="keySize">The size of the key.</param>
        internal RSAPair(string publicKey, int keySize)
        {
            Private = PRIVATE_KEY_UNKNOWN;
            Public = publicKey;
            KeySize = keySize;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The size of the RSA keys.
        /// </summary>
        public int KeySize { get; private set; }

        /// <summary>
        /// The public RSA key.
        /// </summary>
        public string Public { get; private set; }

        /// <summary>
        /// The private RSA key.
        /// </summary>
        public string Private { get; private set; }

        /// <summary>
        /// Whether the current <see cref="RSAPair"/> has a valid public key.
        /// </summary>
        public bool HasPublicKey => !string.IsNullOrWhiteSpace(Public);

        /// <summary>
        /// Whether the current <see cref="RSAPair"/> has a valid private key.
        /// </summary>
        public bool HasPrivateKey => !string.IsNullOrWhiteSpace(Private) && Private != PRIVATE_KEY_UNKNOWN;

        #endregion Properties
    }
}