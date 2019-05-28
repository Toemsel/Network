#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-24-2015
//
// Last Modified By : Thomas
// Last Modified On : 07-29-2015
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
using System.Collections.Generic;

namespace Network
{
    /// <summary>
    /// Same as a .net dictionary. But just working in both directions.
    /// </summary>
    /// <typeparam name="T">The type of the first dictionary.</typeparam>
    /// <typeparam name="U">The type of the second dictionary.</typeparam>
    internal class BiDictionary<T, U>
    {
        private Dictionary<T, U> dictOne = new Dictionary<T, U>();
        private Dictionary<U, T> dictTwo = new Dictionary<U, T>();

        /// <summary>
        /// Gets the ElementA
        /// </summary>
        /// <param name="u">ElementB</param>
        /// <returns>ElementA</returns>
        public T this[U u]
        {
            get
            {
                if(dictTwo.ContainsKey(u))
                    return dictTwo[u];

                return default(T);
            }
            set
            {
                if (!dictTwo.ContainsKey(u))
                    dictTwo.Add(u, value);
                if (!dictOne.ContainsKey(value))
                    dictOne.Add(value, u);

                dictOne[value] = u;
                dictTwo[u] = value;
            }
        }

        /// <summary>
        /// Gets the ElementB
        /// </summary>
        /// <param name="t">ElementA</param>
        /// <returns>ElementB</returns>
        public U this[T t]
        {
            get
            {
                if(dictOne.ContainsKey(t))
                    return dictOne[t];

                return default(U);
            }
            set
            {
                if (!dictOne.ContainsKey(t))
                    dictOne.Add(t, value);
                if (!dictTwo.ContainsKey(value))
                    dictTwo.Add(value, t);

                dictTwo[value] = t;
                dictOne[t] = value;
            }
        }

        /// <summary>
        /// The keys held in the dictionary.
        /// </summary>
        internal ICollection<T> Keys => dictOne.Keys;

        /// <summary>
        /// The values held in the dictionary.
        /// </summary>
        internal ICollection<U> Values => dictOne.Values;

        public bool ContainsKey(T t) => dictOne.ContainsKey(t);

        public bool ContainsKey(U u) => dictTwo.ContainsKey(u);

        public bool ContainsValue(U u) => dictOne.ContainsValue(u);

        public bool ContainsValue(T t) => dictTwo.ContainsValue(t);
    }
}