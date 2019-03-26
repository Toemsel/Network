#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 08-05-2015
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
    /// Enumerates the possible types of <see cref="Connection"/>.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// The <see cref="Connection"/> should use TCP to communicate across
        /// the network.
        /// </summary>
        TCP,

        /// <summary>
        /// The <see cref="Connection"/> should use UDP to communicate across
        /// the network.
        /// </summary>
        UDP,

        /// <summary>
        /// The <see cref="Connection"/> should use Bluetooth to communicate
        /// across the network.
        /// </summary>
        Bluetooth
    }
}