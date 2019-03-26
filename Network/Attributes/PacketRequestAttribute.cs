#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-10-2016
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

using Network.Packets;
using System;

namespace Network.Attributes
{
    /// <summary>
    /// Maps a request packet to the response packet that handles it. This
    /// attribute should be placed on the response packet (must inherit from
    /// <see cref="ResponsePacket"/> and the <see cref="Type"/> of the
    /// <see cref="RequestPacket"/> that it
    /// handles should be given.
    /// </summary>
    public class PacketRequestAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// The <see cref="Type"/> of the <see cref="RequestPacket"/> that the
        /// <see cref="ResponsePacket"/> handles.
        /// </summary>
        public Type RequestType { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the
        /// <see cref="PacketRequestAttribute"/> class with the given
        /// <see cref="RequestPacket"/> type as the handled <see cref="Type"/>.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> of the <see cref="RequestPacket"/> that the
        /// decorated <see cref="ResponsePacket"/> should handle.
        /// </param>
        public PacketRequestAttribute(Type type)
        {
            RequestType = type;
        }

        #endregion Constructors
    }
}