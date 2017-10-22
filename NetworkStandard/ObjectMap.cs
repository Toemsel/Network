#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-10-2016
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
using System.Collections.Generic;
using Network.Extensions;
using Network.Interfaces;
using Network.Packets;
using Network.UID;

namespace Network
{
    /// <summary>
    /// Is able to map objects to their unique ID and back.
    /// Also stores the object's handlers.
    /// </summary>
    internal class ObjectMap
    {
        private Dictionary<Type, Dictionary<object, int>> type_object_id = new Dictionary<Type, Dictionary<object, int>>();
        private Dictionary<int, Tuple<Delegate, object>> id_methodInfo_object = new Dictionary<int, Tuple<Delegate, object>>();
        private Dictionary<string, Delegate> string_methodInfo = new Dictionary<string, Delegate>();

        /// <summary>
        /// Gets the <see cref="Tuple{MethodInfo, System.Object}"/> with the specified identifier.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>Tuple&lt;MethodInfo, System.Object&gt;.</returns>
        internal Delegate this[int ID] { get { return id_methodInfo_object[ID].Item1; } }

        /// <summary>
        /// Gets the <see cref="MethodInfo"/> with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>MethodInfo.</returns>
        internal Delegate this[Type type] { get { return id_methodInfo_object[type_object_id[type].Values.GetEnumerator().ToList<int>()[0]].Item1; } }

        /// <summary>
        /// Gets the static raw data delegate for the given key.
        /// </summary>
        /// <param name="key">The delegate representation.</param>
        /// <returns>A delegate.</returns>
        internal Delegate this[string key] { get { return string_methodInfo[key]; } }

        /// <summary>
        /// Gets the <see cref="System.Int32"/> with the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        /// <returns>System.Int32.</returns>
        internal int this[Type type, object obj] { get { return type_object_id[type][obj]; } }

        /// <summary>
        /// Determines whether the specified packet has someone who is going to handle it.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <returns><c>true</c> if the specified packet has handle; otherwise, <c>false</c>.</returns>
        internal bool HasHandle(Packet packet)
        {
            return id_methodInfo_object.ContainsKey(packet.ID);
        }

        /// <summary>
        /// Registers a packetHandler for raw data requests.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        internal void RegisterStaticRawDataHandler<T>(string key, PacketReceivedHandler<T> handler) where T : Packet
        {
            if(string_methodInfo.ContainsKey(key))
                return; //Never register a string key twice.
            string_methodInfo.Add(key, handler);
        }

        /// <summary>
        /// Removes a raw data handler.
        /// </summary>
        /// <param name="key"></param>
        internal void UnRegisterStaticRawDataHandler(string key)
        {
            if(!string_methodInfo.ContainsKey(key))
                return; //Never unregister a string key which does not exist
            string_methodInfo.Remove(key);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        internal void RegisterStaticPacketHandler<T>(PacketReceivedHandler<T> handler) where T : Packet
        {
            RegisterStaticPacketHandler<T>((Delegate)handler);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="del">The handler which should be invoked.</param>
        internal void RegisterStaticPacketHandler<T>(Delegate del) where T : Packet
        {
            RegisterPacketHandler<T>(del, new object());
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        internal void RegisterPacketHandler<T>(PacketReceivedHandler<T> handler, object obj) where T : Packet
        {
            RegisterPacketHandler<T>((Delegate)handler, obj);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="del">The delegate.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        internal void RegisterPacketHandler<T>(Delegate del, object obj) where T : Packet
        {
            if (type_object_id.ContainsKey(typeof(T)))
                if (type_object_id[typeof(T)].ContainsKey(obj))
                    return; //Don't register a method for a object twice.

            if (!type_object_id.ContainsKey(typeof(T)))
                type_object_id.Add(typeof(T), new Dictionary<object, int>());

            type_object_id[typeof(T)].Add(obj, Generator.UniqueIdentifier<int>());
            id_methodInfo_object.Add(Generator.LastUniqueIdentifier<int>(), new Tuple<Delegate, object>(del, obj));
        }

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        internal void UnRegisterStaticPacketHandler<T>() where T : Packet
        {
            UnRegisterPacketHandler<T>(null);
        }

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        internal void UnRegisterPacketHandler<T>(object obj) where T : Packet
        {
            if (!type_object_id.ContainsKey(typeof(T)) ||
                (obj != null && !type_object_id[typeof(T)].ContainsKey(obj)))
                    return; //The method does not exist to unregister.

            if(obj == null && typeof(T).IsSubclassOf(typeof(RequestPacket)))
            {
                int tempId = type_object_id[typeof(T)].Values.GetEnumerator().ToList<int>()[0];
                type_object_id[typeof(T)].Clear();
                id_methodInfo_object.Remove(tempId);
                return;
            }

            int id = type_object_id[typeof(T)][obj];
            type_object_id[typeof(T)].Remove(obj);
            if (type_object_id[typeof(T)].Count == 0)
                type_object_id.Remove(typeof(T));
            id_methodInfo_object.Remove(id);
        }
    }
}
