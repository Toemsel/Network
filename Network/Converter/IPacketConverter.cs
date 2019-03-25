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
    /// Describes the methods that a packet converter must implement in order
    /// to be able to serialise and deserialise packets to and from a binary
    /// form.
    /// </summary>
    public interface IPacketConverter
    {
        /// <summary>
        /// Serialises a given packet to a byte array.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>System.Byte[].</returns>
        byte[] SerialisePacket(Packet packet);

        /// <summary>
        /// Serialises the given packet of the given type to a byte array.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet to serialise into a byte array.
        /// </typeparam>
        /// <param name="packet">
        /// The packet object to serialise into a byte array.
        /// </param>
        /// <returns>
        /// An array of <see cref="byte"/>s that holds the serialised packet.
        /// </returns>
        byte[] SerialisePacket<P>(P packet) where P : Packet;

        /// <summary>
        /// Deserialises the given data byte array into an object of the given
        /// type.
        /// </summary>
        /// <param name="packetType">
        /// The type of object to deserialise the byte array to.
        /// </param>
        /// <param name="serialisedPacket">
        /// The byte array holding the serialised packet.
        /// </param>
        /// <returns>
        /// The deserialised packet object of the given type.
        /// </returns>
        Packet DeserialisePacket(Type packetType, byte[] serialisedPacket);

        /// <summary>
        /// Deserialises the given byte array into an object of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type to which to deserialise the given byte array.
        /// </typeparam>
        /// <param name="serialisedPacket">
        /// The byte array holding the serialised packet.
        /// </param>
        /// <returns>
        /// The deserialised packet object, of the given type.
        /// </returns>
        P DeserialisePacket<P>(byte[] serialisedPacket) where P : Packet;
    }
}