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
using System.Collections;
using System.Collections.Generic;

namespace Network.Extensions
{
    /// <summary>
    /// IEnumerator extensions.
    /// </summary>
    internal static class EnumeratorExtension
    {
        /// <summary>
        /// Reads all available elements from an enumerator and inserts it into a collection.
        /// </summary>
        /// <typeparam name="T">Type of the collection.</typeparam>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns>List&lt;T&gt;.</returns>
        internal static List<T> ToList<T>(this IEnumerator enumerator)
        {
            List<T> collection = new List<T>();
            while (enumerator.MoveNext())
                collection.Add((T)enumerator.Current);
            return collection;
        }
    }
}
