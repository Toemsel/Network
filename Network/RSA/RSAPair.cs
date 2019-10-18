using System;
using System.Diagnostics;

namespace Network.RSA
{
    /// <summary>
    /// Stores an RSA public/private key-pair.
    /// </summary>
    public class RSAPair
    {
        #region Variables        
        /// <summary>
        /// The max packet size to encrypt.
        /// </summary>
        private readonly int encryptionByteSize;
        /// <summary>
        /// The max packet size to decrypt.
        /// </summary>
        private readonly int decryptionByteSize;

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
        public RSAPair(string publicKey, string privateKey, int keySize) : this(publicKey, privateKey, keySize, Connection.IsXpOrHigher) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class. Used for internal purposes. The communication
        /// partner only sends us his public key to encrypt.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="eanbleOAEPadding">Use OAE Padding.</param>
        internal RSAPair(string publicKey, int keySize, bool eanbleOAEPadding) : this(publicKey, PRIVATE_KEY_UNKNOWN, keySize, eanbleOAEPadding) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAPair"/> class. Used for internal purposes. The communication
        /// partner only sends us his public key to encrypt.
        /// </summary>
        /// <param name="publicKey">The public key (https://superdry.apphb.com/tools/online-rsa-key-converter).</param>
        /// <param name="privateKey">The private key (https://superdry.apphb.com/tools/online-rsa-key-converter).</param>
        /// <param name="keySize">The size of the key.</param>
        /// <param name="eanbleOAEPadding">Use OAE Padding.</param>
        private RSAPair(string publicKey, string privateKey, int keySize, bool eanbleOAEPadding)
        {
            if (keySize > 16384 || keySize < 384 || keySize % 8 != 0)
                throw new ArgumentException($"KeySize '{keySize}' isn't a valid RSA key length." +
                    $"Key-requirements: {nameof(keySize)} >= 384 && {nameof(keySize)} <= 16384 && {nameof(keySize)} % 8 == 0." +
                    $"E.g.: 512, 1024, 2048, 4096,...");

            if (KeySize > 4096)
                Trace.TraceWarning($"{nameof(keySize)} exceeds the size of 4096 bits. Expect a major impact in performance.");

            Private = privateKey;
            Public = publicKey;
            KeySize = keySize;

            EnableOAEPadding = eanbleOAEPadding && Connection.IsXpOrHigher;

            encryptionByteSize = EnableOAEPadding ? 
                ((keySize - 384) / 8) + 6 :
                ((keySize - 384) / 8) + 37;
            decryptionByteSize = keySize / 8;
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
        /// Gets a value indicating whether OAE Padding is enabled.
        /// </summary>
        /// <value><c>true</c> if [enable oae padding]; otherwise, <c>false</c>.</value>
        public bool EnableOAEPadding { get; private set; }

        /// <summary>
        /// Gets the max size of an decrypted packet.
        /// </summary>
        /// <value>The size of the decryption byte.</value>
        public int DecryptionByteSize => decryptionByteSize;

        /// <summary>
        /// Gets the max size of an encrypted packet.
        /// </summary>
        /// <value>The size of the encryption byte.</value>
        public int EncryptionByteSize => encryptionByteSize;

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