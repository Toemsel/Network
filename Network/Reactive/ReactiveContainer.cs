#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 12-03-2017
//
// Last Modified By : Thomas
// Last Modified On : 12-03-2017
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
using Network.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Network.Reactive
{
    /// <summary>
    /// A container class to store static values for reuse.
    /// </summary>
    internal class ReactiveContainer
    {
        private static ReactiveContainer reactiveContainer;

        private Dictionary<Type, PropertyInfo[]> reactiveObjects = new Dictionary<Type, PropertyInfo[]>();
        private Dictionary<PropertyInfo, SyncAttribute> syncAttributes = new Dictionary<PropertyInfo, SyncAttribute>();
        private Dictionary<PropertyInfo, PacketIgnorePropertyAttribute> packetIgnoreAttributes = new Dictionary<PropertyInfo, PacketIgnorePropertyAttribute>();

        private ReactiveContainer() { }

        /// <summary>
        /// Gathers the propertyInfos of a given type.
        /// </summary>
        /// <param name="type">The type to gather the propertyInfos.</param>
        /// <returns>PropertyInfos.</returns>
        internal PropertyInfo[] this[Type type]
        {
            get
            {
                if (!reactiveObjects.ContainsKey(type))
                    reactiveObjects.Add(type, type.GetProperties().Where(p => !PacketIngoreAttributeFlag(p)).ToArray());
                return reactiveObjects[type];
            }
        }

        internal PropertyInfo this[Type type, string callerName] => this[type]?.SingleOrDefault(p => p.Name == callerName);

        /// <summary>
        /// Searches for a possible sync attribute.
        /// </summary>
        /// <param name="propertyInfo">The attribute to search for.</param>
        /// <returns>[obj] if existent. [NULL] else.</returns>
        internal SyncAttribute this[PropertyInfo propertyInfo]
        {
            get
            {
                if (propertyInfo == null)
                    return null;

                if (!syncAttributes.ContainsKey(propertyInfo))
                    syncAttributes.Add(propertyInfo, propertyInfo.GetCustomAttribute<SyncAttribute>());
                return syncAttributes[propertyInfo];
            }
        }

        /// <summary>
        /// Checks if the property is flagged as "to be ignored".
        /// </summary>
        /// <param name="propertyInfo">The property to check for.</param>
        /// <returns>[True] if it should be ignored. [False] if it shouldn't be ignored.</returns>
        internal bool PacketIngoreAttributeFlag(PropertyInfo propertyInfo)
        {
            if (!packetIgnoreAttributes.ContainsKey(propertyInfo))
                packetIgnoreAttributes.Add(propertyInfo, propertyInfo.GetCustomAttribute<PacketIgnorePropertyAttribute>());
            return packetIgnoreAttributes[propertyInfo] != null;
        }

        /// <summary>
        /// Singleton instance of a reactiveContainer.
        /// </summary>
        internal static ReactiveContainer Singleton => reactiveContainer = reactiveContainer ?? new ReactiveContainer();
    }
}
