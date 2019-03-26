#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 30.09.2018
//
// Last Modified By : Thomas
// Last Modified On : 30.09.2018
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

using Network.Attributes;

namespace Network.Packets.RSA
{
    /// <summary>
    /// Response packet for a <see cref="RSAKeyInformationRequest"/>.
    /// </summary>
    [PacketType(12), PacketRequest(typeof(RSAKeyInformationRequest))]
    internal class RSAKeyInformationResponse : ResponsePacket
    {
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

        #region Constructors

        public RSAKeyInformationResponse(
            string publicKey, int keySize, RSAKeyInformationRequest request)
            : base(request)
        {
            PublicKey = publicKey;
            KeySize = keySize;
        }

        #endregion Constructors
    }
}