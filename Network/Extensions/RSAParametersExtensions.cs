using System;
using System.Security.Cryptography;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="RSAParameters"/>
    /// struct.
    /// </summary>
    internal static class RSAParametersExtensions
    {
        #region Methods

        /// <summary>
        /// Extracts the private key from the given <see cref="RSAParameters"/>
        /// and returns it as an XML string.
        /// </summary>
        /// <param name="rsaParameter">
        /// The <see cref="RSAParameters"/> instance that the extension method
        /// affects.
        /// </param>
        /// <returns>
        /// The XML string with the private key.
        /// </returns>
        public static string ExtractPrivateKey(this RSAParameters rsaParameter)
        {
            return
                $"<RSAKeyValue>" +
                $"<Modulus>{Convert.ToBase64String(rsaParameter.Modulus)}</Modulus>" +
                $"<Exponent>{Convert.ToBase64String(rsaParameter.Exponent)}</Exponent>" +
                $"<P>{Convert.ToBase64String(rsaParameter.P)}</P>" +
                $"<Q>{Convert.ToBase64String(rsaParameter.Q)}</Q>" +
                $"<DP>{Convert.ToBase64String(rsaParameter.DP)}</DP>" +
                $"<DQ>{Convert.ToBase64String(rsaParameter.DQ)}</DQ>" +
                $"<InverseQ>{Convert.ToBase64String(rsaParameter.InverseQ)}</InverseQ>" +
                $"<D>{Convert.ToBase64String(rsaParameter.D)}</D>" +
                $"</RSAKeyValue>";
        }

        /// <summary>
        /// Extracts the public key from the given <see cref="RSAParameters"/>
        /// and returns it as an XML string.
        /// </summary>
        /// <param name="rsaParameter">
        /// The <see cref="RSAParameters"/> instance that the extension method
        /// affects.
        /// </param>
        /// <returns>
        /// The XML string with the public key.
        /// </returns>
        public static string ExtractPublicKey(this RSAParameters rsaParameter)
        {
            return
                $"<RSAKeyValue>" +
                $"<Modulus>{Convert.ToBase64String(rsaParameter.Modulus)}</Modulus>" +
                $"<Exponent>{Convert.ToBase64String(rsaParameter.Exponent)}</Exponent>" +
                $"</RSAKeyValue>";
        }

        #endregion Methods
    }
}