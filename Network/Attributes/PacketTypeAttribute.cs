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

using Network.Packets;
using Network.Packets.RSA;
using System;

namespace Network.Attributes
{
    /// <summary>
    /// To identify every packet server and client side, a unique identifier is needed.
    /// Mark every packet class with this attribute and set a unique id. (UInt16)
    /// 2^16 (65536) unique ids are possible. Double usage of one id will lead to an exception.
    /// <list type="table">
    /// <listheader>
    /// <description>
    /// Following ids are already taken by the network lib:
    /// </description>
    /// </listheader>
    /// <item>
    /// <term>0</term>
    /// <description>
    /// <see cref="PingRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>1</term>
    /// <description>
    /// <see cref="PingResponse"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>2</term>
    /// <description>
    /// <see cref="CloseRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>3</term>
    /// <description>
    /// <see cref="EstablishUdpRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>4</term>
    /// <description>
    /// <see cref="EstablishUdpResponse"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>5</term>
    /// <description>
    /// <see cref="EstablishUdpResponseACK"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>6</term>
    /// <description>
    /// <see cref="AddPacketTypeRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>7</term>
    /// <description>
    /// <see cref="AddPacketTypeResponse"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>8</term>
    /// <description>
    /// <see cref="UDPPingRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>9</term>
    /// <description>
    /// <see cref="UDPPingResponse"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>10</term>
    /// <description>
    /// <see cref="RawData"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>11</term>
    /// <description>
    /// <see cref="RSAKeyInformationRequest"/>
    /// </description>
    /// </item>
    /// <item>
    /// <term>12</term>
    /// <description>
    /// <see cref="RSAKeyInformationResponse"/>
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Knowledge about the ID isn't essential anymore (Since version 2.0.0.0).
    /// However, the above IDs should NOT be overwritten, for compatibility
    /// purposes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketTypeAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// The ID to use for the decorated <see cref="Packet"/>.
        /// </summary>
        public ushort Id { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="PacketTypeAttribute"/>
        /// class, with the given ID to be used for the decorated <see cref="Packet"/>.
        /// </summary>
        /// <param name="packetType">
        /// The ID to use for the decorated <see cref="Packet"/>.
        /// </param>
        public PacketTypeAttribute(ushort packetType)
        {
            Id = packetType;
        }

        #endregion Constructors
    }
}