namespace Network.RSA
{
    /// <summary>
    /// Contains RSA communication properties.
    /// </summary>
    public class RSAPair
    {
        private const string PRIVATE_KEY_UNKNOWN = "UNKNOWN";

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class.
        /// Generate a <see cref="RSAPair" /> with <see cref="RSAKeyGeneration"/>.<see cref="RSAKeyGeneration.Generate(int)" />
        /// </summary>
        /// <param name="publicKey">The public key. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">Size of the key.</param>
        public RSAPair(string publicKey, string privateKey, int keySize)
        {
            Private = privateKey;
            Public = publicKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class.
        /// Used for internal purposes. The communication partner only
        /// sends us his public key to encrypt.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <param name="keySize">Size of the key.</param>
        internal RSAPair(string publicKey, int keySize)
        {
            Private = PRIVATE_KEY_UNKNOWN;
            Public = publicKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Gets or sets the size of the key.
        /// </summary>
        /// <value>The size of the key.</value>
        public int KeySize { get; private set; }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        /// <value>The public.</value>
        public string Public { get; private set; }

        /// <summary>
        /// Gets or sets the private key.
        /// </summary>
        /// <value>The private.</value>
        public string Private { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a public key.
        /// </summary>
        /// <value><c>true</c> if this instance has a public key; otherwise, <c>false</c>.</value>
        public bool HasPublicKey => !string.IsNullOrWhiteSpace(Public);

        /// <summary>
        /// Gets a value indicating whether this instance has a private key.
        /// </summary>
        /// <value><c>true</c> if this instance has a private key; otherwise, <c>false</c>.</value>
        public bool HasPrivateKey => !string.IsNullOrWhiteSpace(Private) && Private != PRIVATE_KEY_UNKNOWN;
    }
}