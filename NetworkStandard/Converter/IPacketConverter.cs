#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-24-2015
//
// Last Modified By : Thomas
// Last Modified On : 03-10-2016
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2016
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

namespace Network.Converter
{
    /// <summary>
    /// Provides extension methods for packets to handle their read and write behaviors.
    /// </summary>
    public interface IPacketConverter
    {
        /// <summary>
        /// Converts a given packet to a byte array.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>System.Byte[].</returns>
        byte[] GetBytes(Packet packet);

        /// <summary>
        /// Converts the given data byte array into a Packet object.
        /// </summary>
        /// <param name="packetType">The type of the packet object.</param>
        /// <param name="data">The data array</param>
        /// <returns>A consumable object.</returns>
        Packet GetPacket(Type packetType, byte[] data);
    }
}
