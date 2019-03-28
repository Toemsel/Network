using Network.Extensions;
using System.Security.Cryptography;

namespace Network.RSA
{
    public static class RSAKeyGeneration
    {
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
    }
}