using Network.Extensions;
using System.Security.Cryptography;

namespace Network.RSA
{
    /// <summary>
    /// Provides helper methods for the generation of RSA key-pairs.
    /// </summary>
    public static class RSAKeyGeneration
    {
        #region Methods

        /// <summary>
        /// Generates and returns a new <see cref="RSAPair"/>.
        /// </summary>
        /// <param name="keySize">The RSA key size to use.</param>
        /// <returns>The unique <see cref="RSAPair"/>.</returns>
        public static RSAPair Generate(int keySize = 2048)
        {
            using (RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(keySize))
            {
                //Do not keep the key in the OS storage.
                cryptoServiceProvider.PersistKeyInCsp = false;

                RSAParameters keyParameters = cryptoServiceProvider.ExportParameters(true);
                return new RSAPair(keyParameters.ExtractPublicKey(), keyParameters.ExtractPrivateKey(), keySize);
            }
        }

        #endregion Methods
    }
}