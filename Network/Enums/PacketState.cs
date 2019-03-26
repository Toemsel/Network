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

namespace Network.Enums
{
    /// <summary>
    /// Enumerates the possible states that a <see cref="Packet"/> could be in
    /// after transmission.
    /// </summary>
    public enum PacketState
    {
        /// <summary>
        /// The packet was successfully transmitted and received.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The packet was not received within the specified timeout. The
        /// <see cref="Connection"/> could be dead.
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// The <see cref="Connection"/> is not alive, so no asynchronous
        /// transmission is possible.
        /// </summary>
        ConnectionNotAlive = 2
    }
}