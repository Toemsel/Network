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

namespace Network.Interfaces
{
    /// <summary>
    /// If this instance received a packet, this delegate will be used to deliver the packet and
    /// the receiving network instance.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="connection">The connection.</param>
    public delegate void PacketReceivedHandler<T>(T packet, Connection connection) where T : Packet;

    /// <summary>
    /// Provides the basic methods to register and unregister methods.
    /// </summary>
    public interface IPacketHandler
    {
        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        void RegisterStaticPacketHandler<T>(PacketReceivedHandler<T> handler) where T : Packet;

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        void RegisterPacketHandler<T>(PacketReceivedHandler<T> handler, object obj) where T : Packet;

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        void UnRegisterStaticPacketHandler<T>() where T : Packet;

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        void UnRegisterPacketHandler<T>(object obj) where T : Packet;
    }
}
