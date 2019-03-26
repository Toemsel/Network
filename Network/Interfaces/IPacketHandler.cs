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

namespace Network.Interfaces
{
    /// <summary>
    /// Represents a method that handles receiving a <see cref="Packet"/> of
    /// the given type on the given <see cref="Connection"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="Packet"/> that the delegate should handle.
    /// </typeparam>
    /// <param name="packet">
    /// The received <see cref="Packet"/> object.
    /// </param>
    /// <param name="connection">
    /// The <see cref="Connection"/> that received the packet.
    /// </param>
    public delegate void PacketReceivedHandler<T>(
        T packet, Connection connection) where T : Packet;

    /// <summary>
    /// Describes the methods a class must implement to handle <see cref="Packet"/>s.
    /// </summary>
    public interface IPacketHandler
    {
        #region Methods

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{T}"/> for all
        /// <see cref="Packet"/>s of the given type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="Packet"/> the delegate should handle.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="PacketReceivedHandler{T}"/> delegate to be invoked
        /// for each received packet of the given type.
        /// </param>
        void RegisterStaticPacketHandler<T>(PacketReceivedHandler<T> handler)
            where T : Packet;

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{T}"/> on the
        /// given <see cref="object"/> for all <see cref="Packet"/>s of the given type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="Packet"/> the delegate should handle.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="PacketReceivedHandler{T}"/> delegate to be invoked
        /// for each received packet of the given type.
        /// </param>
        /// <param name="obj">
        /// The <see cref="object"/> that should receive the <see cref="Packet"/>s.
        /// </param>
        void RegisterPacketHandler<T>(PacketReceivedHandler<T> handler, object obj)
            where T : Packet;

        /// <summary>
        /// Deregisters all <see cref="PacketReceivedHandler{T}"/>s for the given
        /// <see cref="Packet"/> type.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="Packet"/> for which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s should be deregistered.
        /// </typeparam>
        void DeregisterStaticPacketHandler<T>() where T : Packet;

        /// <summary>
        /// Deregisters all <see cref="PacketReceivedHandler{T}"/>s for the given
        /// <see cref="Packet"/> type that are currently registered on the given
        /// <see cref="object"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of <see cref="Packet"/> for which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s should be deregistered.
        /// </typeparam>
        /// <param name="obj">
        /// The <see cref="object"/> on which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s of the given type should
        /// be deregistered.
        /// </param>
        void DeregisterPacketHandler<T>(object obj) where T : Packet;

        #endregion Methods
    }
}