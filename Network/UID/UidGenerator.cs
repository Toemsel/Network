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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Network.UID
{
    /// <summary>
    /// Provides methods for the generation of unique identifiers for objects.
    /// </summary>
    internal static class UidGenerator
    {
        #region Variables

        /// <summary>
        /// Maps a <see cref="Type"/> to its cached, unique ID via a thread-safe
        /// dictionary.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> typeToIdMap =
            new ConcurrentDictionary<Type, object>();

        #endregion Variables

        #region Methods

        /// <summary>
        /// Generates a unique identifier for the given
        /// </summary>
        /// <typeparam name="T">
        /// The type for which to generate a unique ID.
        /// </typeparam>
        /// <returns>
        /// The unique ID.
        /// </returns>
        public static T UniqueIdentifier<T>()
        {
            Type type = typeof(T);

            typeToIdMap.AddOrUpdate(type, default(T), (_, value) =>
            {
                dynamic currentValue = value;
                return ++currentValue;
            });

            return (T)typeToIdMap[type];
        }

        /// <summary>
        /// Returns the unique identifier associated with the given type.
        /// </summary>
        /// <typeparam name="T">
        /// The type whose ID to get.
        /// </typeparam>
        /// <returns>
        /// The unique ID.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the method is called before an ID is present. This occurs
        /// if a previous call to <see cref="UniqueIdentifier{T}"/> was not made
        /// for the given type, and thus no ID actually exists.
        /// </exception>
        public static T LastUniqueIdentifier<T>()
        {
            Type type = typeof(T);

            // if we call the LastUniqueIdentifier before we actually
            // filled the dictionary, there was no last UID. Therefore exception.
            if (!typeToIdMap.ContainsKey(type))
            {
                throw new KeyNotFoundException();
            }

            return (T)typeToIdMap[type];
        }

        #endregion Methods
    }
}