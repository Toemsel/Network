#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 29.09.2018
//
// Last Modified By : Thomas
// Last Modified On : 29.09.2018
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

using Network.RSA;

using System.Security.Cryptography;

namespace Network.Interfaces
{
    /// <summary>
    /// A connection supporting RSA en/decryption.
    /// </summary>
    internal interface IRSAConnection : IRSACapability
    {
        /// <summary>
        /// Gets or sets the encryption provider.
        /// </summary>
        /// <value>The encryption provider.</value>
        RSACryptoServiceProvider EncryptionProvider { get; set; }

        /// <summary>
        /// Gets or sets the decryption provider.
        /// </summary>
        /// <value>The decryption provider.</value>
        RSACryptoServiceProvider DecryptionProvider { get; set; }

        /// <summary>
        /// Gets or sets the communication partner's RSA pair.
        /// </summary>
        /// <value>The communication partner RSA pair.</value>
        RSAPair CommunicationPartnerRSAPair { get; set; }

        /// <summary>
        /// Encrypts bytes with the <see cref="RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The Bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        byte[] Encrypt(byte[] bytes);

        /// <summary>
        /// Decrypt bytes with the <see cref="RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The bytes to decrypt.</param>
        /// <returns>The decrypted bytes.</returns>
        byte[] Decrypt(byte[] bytes);
    }
}