#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-03-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2015
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
using System.Threading;

namespace Network.Extensions
{
    /// <summary>
    /// Connection extensions. Provides some methods to handle a connection.
    /// </summary>
    internal static class ConnectionExtension
    {
        private static int counter;

        /// <summary>
        /// Generates a unique hashCode for the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns>System.Int32.</returns>
        internal static int GenerateUniqueHashCode(this Connection connection)
        {
            return Interlocked.Increment(ref counter);
        }
    }
}
