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
    /// Packet to inform the communication partner about the public-key.
    /// </summary>
    /// <seealso cref="Network.Packet" />
    [PacketType(21)]
    internal class RSAKeyInformationRequest : RequestPacket
    {
        public RSAKeyInformationRequest()
        {
        }

        public RSAKeyInformationRequest(string publicKey, int keySize)
        {
            PublicKey = publicKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        /// <value>The public key.</value>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the size of the key.
        /// </summary>
        /// <value>The size of the key.</value>
        public int KeySize { get; set; }
    }
}