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

using System;
using System.Collections.Generic;

namespace Network.UID
{
    /// <summary>
    /// Provides functions to generate unique identifierts.
    /// </summary>
    internal static class Generator
    {
        private static object lockObject = new object();
        private static Dictionary<Type, object> values = new Dictionary<Type, object>();

        /// <summary>
        /// Generates an unique identifier.
        /// </summary>
        /// <typeparam name="T">The type of the identifier.</typeparam>
        /// <returns>The unique identifier.</returns>
        internal static T UniqueIdentifier<T>()
        {
            lock (lockObject)
            {
                if (!values.ContainsKey(typeof(T)))
                    values.Add(typeof(T), default(T));

                dynamic currentValue = values[typeof(T)];
                values[typeof(T)] = ++currentValue;
                return (T)values[typeof(T)];
            }
        }

        /// <summary>
        /// Returns the last unique identifier of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the identifier.</typeparam>
        /// <returns>The last generated value.</returns>
        /// <exception cref="System.NotSupportedException">No last value available.</exception>
        internal static T LastUniqueIdentifier<T>()
        {
            lock (lockObject)
            {
                //If we call the LastUniqueIdentifier before we actually
                //filled the dictionary, there was no last UID. Therefore exception.
                if (!values.ContainsKey(typeof(T)))
                    throw new KeyNotFoundException();
                return (T)values[typeof(T)];
            }
        }
    }
}