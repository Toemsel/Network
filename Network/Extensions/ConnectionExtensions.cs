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

using System.Threading;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="Connection"/>
    /// class.
    /// </summary>
    internal static class ConnectionExtensions
    {
        #region Variables

        /// <summary>
        /// A private thread-safe counter for generating unique hash codes.
        /// </summary>
        /// <remarks>
        /// Increments are guaranteed to be atomic on all 32-bit and higher
        /// systems, as any single-instruction operation on a variable is
        /// by definition atomic. Since an <see cref="int"/> is 32 bits long,
        /// it can be loaded with 1 instruction into a register on a 32-bit or
        /// higher system. Likewise, addition is also atomic. This guarantees
        /// atomic behaviour for increments on an <see cref="int"/>.
        /// </remarks>
        private static int counter;

        #endregion Variables

        #region Methods

        /// <summary>
        /// Generates a new unique hash code for the <see cref="Connection"/> via
        /// a thread-safe increment operation.
        /// </summary>
        /// <param name="connection">
        /// The <see cref="Connection"/> instance this extension method affects.
        /// </param>
        /// <returns>
        /// A new, unique hash code.
        /// </returns>
        /// <remarks>
        /// This method is thread safe, see <see cref="counter"/> for more info.
        /// </remarks>
        public static int GenerateUniqueHashCode(this Connection connection)
        {
            return Interlocked.Increment(ref counter);
        }

        #endregion Methods
    }
}