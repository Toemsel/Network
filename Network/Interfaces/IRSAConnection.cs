using Network.Packets;
using Network.RSA;
using System.Security.Cryptography;

namespace Network.Interfaces
{
    /// <summary>
    /// Describes the properties and methods that a class must implement to be able to communicate via secure, RSA encrypted messages.
    /// </summary>
    internal interface IRSAConnection : IRSACapability
    {
        #region Properties

        /// <summary>
        /// Provides RSA encryption services, to encrypt serialised <see cref="Packet"/>s.
        /// </summary>
        RSACryptoServiceProvider EncryptionProvider { get; set; }

        /// <summary>
        /// Provides RSA decryption services, to decrypt serialised <see cref="Packet"/>s.
        /// </summary>
        RSACryptoServiceProvider DecryptionProvider { get; set; }

        /// <inheritdoc cref="IRSACapability.RSAPair"/>
        RSAPair CommunicationPartnerRSAPair { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Encrypts the given byte array using the <see cref="EncryptionProvider"/> and returns the encrypted version.
        /// </summary>
        /// <param name="bytes">The original, plaintext byte array.</param>
        /// <returns>The encrypted byte array.</returns>
        byte[] EncryptBytes(byte[] bytes);

        /// <summary>
        /// Decrypts the given byte array using the <see cref="DecryptionProvider"/> and returns the plaintext version.
        /// </summary>
        /// <param name="bytes">The encrypted byte array.</param>
        /// <returns>The original, plaintext byte array.</returns>
        byte[] DecryptBytes(byte[] bytes);

        #endregion Methods
    }
}