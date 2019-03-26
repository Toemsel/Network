#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
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

using System.Collections.Generic;

namespace Network.Packets
{
    /// <summary>
    /// Response packet for the <see cref="AddPacketTypeRequest"/> packet.
    /// </summary>
    [PacketType(7), PacketRequest(typeof(AddPacketTypeRequest))]
    internal class AddPacketTypeResponse : ResponsePacket
    {
        #region Properties

        /// <summary>
        /// List of all the local <see cref="Packet"/> IDs that have been registered.
        /// </summary>
        public List<ushort> LocalDict { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPacketTypeResponse"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="request">The request.</param>
        public AddPacketTypeResponse(List<ushort> dictionary, AddPacketTypeRequest request)
            : base(request)
        {
            LocalDict = dictionary;
        }

        #endregion Constructors
    }
}