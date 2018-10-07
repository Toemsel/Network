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
    /// Represents the state of the packet.
    /// </summary>
    public enum PacketState : int
    {
        /// <summary>
        /// The packet was successfully transmitted.
        /// </summary>
        Success = 0,
        /// <summary>
        /// No result received. Timeout limit reached.
        /// Connection may be already dead.
        /// </summary>
        Timeout = 1,
        /// <summary>
        /// The connection is not alive. No async transmission possible.
        /// </summary>
        ConnectionNotAlive = 2
    }
}