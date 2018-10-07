#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 07-26-2015
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
using System;

namespace Network.Attributes
{
    /// <summary>
    /// To identify every packet server and client side, a unique identifier is needed.
    /// Mark every packet class with this attribute and set a unique id. (UInt16)
    /// 2^16 (65536) unique ids are possible. Double usage of one id will lead to an exception.
    /// Following ids are already taken by the network lib:
    /// - (0)   PingRequest
    /// - (1)   PingResponse
    /// - (2)   CloseRequest
    /// - (3)   EstablishUdpRequest
    /// - (4)   EstablishUdpResponse
    /// - (5)   EstablishUdpResponseACK
    /// - (6)   VersionRequest
    /// - (7)   VersionResponse
    /// - (8)   AddPacketTypeRequest
    /// - (9)   AddPacketTypeResponse
    /// - (10)  UDPPingRequest
    /// - (11)  UDPPingResponse
    /// Knowledge about the ID isn't essential anymore. (Since version 2.0.0.0)
    /// Just do not overwrite the existing IDs above.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketTypeAttribute : Attribute
    {
        public PacketTypeAttribute(UInt16 packetType)
        {
            Id = packetType;
        }

        public UInt16 Id { get; private set; }
    }
}
