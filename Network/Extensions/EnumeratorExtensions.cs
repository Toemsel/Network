#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-25-2015
//
// Last Modified By : Thomas
// Last Modified On : 07-25-2015
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

using System.Collections;
using System.Collections.Generic;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="IEnumerator"/>
    /// interface.
    /// </summary>
    internal static class EnumeratorExtensions
    {
        #region Methods

        /// <summary>
        /// Adds each item in the <see cref="IEnumerator"/> into a <see cref="List{T}"/>
        /// and return the new <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the elements in the <see cref="List{T}"/>.
        /// </typeparam>
        /// <param name="enumerator">
        /// The <see cref="IEnumerator"/> instance that the extension method affects.
        /// </param>
        /// <returns>
        /// The <see cref="List{T}"/> instance with the elements of the
        /// <see cref="IEnumerator"/>.
        /// </returns>
        public static List<T> ToList<T>(this IEnumerator enumerator)
        {
            List<T> collection = new List<T>();
            while (enumerator.MoveNext())
                collection.Add((T)enumerator.Current);
            return collection;
        }

        #endregion Methods
    }
}