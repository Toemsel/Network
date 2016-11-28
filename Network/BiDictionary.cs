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
using System.Collections.Generic;
using System.Linq;

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
            get { return dictTwo[u]; }
        }

        /// <summary>
        /// Gets the ElementB
        /// </summary>
        /// <param name="t">ElementA</param>
        /// <returns>ElementB</returns>
        public U this[T t]
        {
            get { return dictOne[t]; }
        }

        /// <summary>
        /// Gets all the values for the A element.
        /// </summary>
        public List<T> AElements { get { return dictTwo.Values.ToList<T>(); } }

        /// <summary>
        /// Gets all the values for the B element.
        /// </summary>
        public List<U> BElements { get { return dictOne.Values.ToList<U>(); } }

        /// <summary>
        /// Adds an element to the bidictinary
        /// </summary>
        /// <param name="t">Element A</param>
        /// <param name="u">Element B</param>
        public void Add(T t, U u)
        {
            dictOne.Add(t, u);
            dictTwo.Add(u, t);
        }

        /// <summary>
        /// Removes an element from the bidictinary
        /// </summary>
        /// <param name="t">Element A</param>
        public void RemoveElement(T t)
        {
            dictTwo.Remove(dictOne[t]);
            dictOne.Remove(t);
        }

        /// <summary>
        /// Removes an element from the bidictinary
        /// </summary>
        /// <param name="u">Element B</param>
        public void RemoveElement(U u)
        {
            dictOne.Remove(dictTwo[u]);
            dictTwo.Remove(u);
        }

        /// <summary>
        /// Adds an element to the bidictinary
        /// </summary>
        /// <param name="t">Element B</param>
        /// <param name="u">Element A</param>
        public void AddElement(U u, T t)
        {
            dictOne.Add(t, u);
            dictTwo.Add(u, t);
        }

        /// <summary>
        /// Removes an element from the bidictinary
        /// </summary>
        /// <param name="t">Element B</param>
        /// <param name="u">Element A</param>
        public void RemoveElement(U u, T t)
        {
            dictOne.Remove(t);
            dictTwo.Remove(u);
        }

        /// <summary>
        /// Returns an element
        /// </summary>
        /// <param name="u">Element B</param>
        /// <returns>Element A</returns>
        public T GetElement(U u)
        {
            return dictTwo[u];
        }

        /// <summary>
        /// Returns an element
        /// </summary>
        /// <param name="t">Element A</param>
        /// <returns>Element B</returns>
        public U GetElement(T t)
        {
            return dictOne[t];
        }

        /// <summary>
        /// Returns the element a a special offset
        /// </summary>
        /// <param name="offSet">The offset</param>
        /// <returns>KeyValuePair</returns>
        public KeyValuePair<T, U> GetElement(int offSet)
        {
            return dictOne.ElementAt(offSet);
        }

        /// <summary>
        /// Returns the amount of the dictionary
        /// </summary>
        public int Count
        {
            get { return (dictOne.Count + dictTwo.Count) / 2; }
        }

        /// <summary>
        /// Proofs if an A element exists or not
        /// </summary>
        /// <param name="t">Element A</param>
        /// <returns>Exists or not</returns>
        public bool ContainsKeyA(T t)
        {
            return dictOne.ContainsKey(t);
        }

        /// <summary>
        /// Determines whether [contains value a] [the specified u].
        /// </summary>
        /// <param name="u">The u.</param>
        /// <returns><c>true</c> if [contains value a] [the specified u]; otherwise, <c>false</c>.</returns>
        public bool ContainsValueA(U u)
        {
            return dictOne.ContainsValue(u);
        }

        /// <summary>
        /// Proofs if an A element exists or not
        /// </summary>
        /// <param name="u">Element B</param>
        /// <returns>Exists or not</returns>
        public bool ContainsKeyB(U u)
        {
            return dictTwo.ContainsKey(u);
        }

        /// <summary>
        /// Determines whether [contains value b] [the specified t].
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns><c>true</c> if [contains value b] [the specified t]; otherwise, <c>false</c>.</returns>
        public bool ContainsValueB(T t)
        {
            return dictTwo.ContainsValue(t);
        }
    }
}
