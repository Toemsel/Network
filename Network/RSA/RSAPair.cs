#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 29.09.2018
//
// Last Modified By : Thomas
// Last Modified On : 01.10.2018
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2018
// </copyright>
// <License>
// GNU LESSER GENERAL PUBLIC LICENSE
// </License>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ***********************************************************************
#endregion Licence - LGPLv3
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