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
using Network.Comperators;
using Network.Packets;
using System;
using System.Collections.Generic;

namespace Network.Reactive
{
    /// <summary>
    /// This class contains all reactive objects and their associations.
    /// Its purpose is to map id's to objects and to other way around.
    /// It is responsible to establish new reactive objects and handle
    /// their life states.
    /// </summary>
    internal class Reactive
    {
        private ConcurrentBiDictionary<ReactiveObject, byte[]> reactiveObjects = new ConcurrentBiDictionary<ReactiveObject, byte[]>(new ByteArrayComparer());

        internal Type this[byte[] id] => reactiveObjects[id]?.GetType();

        /// <summary>
        /// Sends a sync request to all given clients.
        /// </summary>
        /// <typeparam name="T">The type of the value to sync.</typeparam>
        /// <param name="reactiveObject">The reactiveObject.</param>
        /// <param name="val">The value to sync.</param>
        /// <param name="propertyName">The propertyName to sync.</param>
        /// <param name="connections">The connections to sync with.</param>
        internal static void ReactiveSyncRequest<T>(ReactiveObject reactiveObject, T val, string propertyName, IEnumerable<Connection> connections)
        {
            var reactiveSyncPacket = new ReactiveSync();
            reactiveSyncPacket.ReactiveObjectId = reactiveObject.ReactiveObjectId;
            reactiveSyncPacket.PropertyName = propertyName;
            reactiveSyncPacket.PropertyValue = val;

            foreach (Connection currentConnection in connections)
                currentConnection.Send(reactiveSyncPacket);
        }
        
        /// <summary>
        /// We received a sync request. We do not check any restrictions or predicates.
        /// We only deliver the new value to the given property.
        /// </summary>
        /// <param name="syncRequest">The sync-Request to process.</param>
        internal void ReactiveSyncRequestReceived(ReactiveSync syncRequest)
        {
            if (reactiveObjects.ContainsKeyB(syncRequest.ReactiveObjectId))
                reactiveObjects[syncRequest.ReactiveObjectId].SyncReceive(syncRequest.PropertyValue, syncRequest.PropertyName);
        }

        internal bool Add(ReactiveObject reactiveObject)
        {
            if (reactiveObjects.ContainsKeyA(reactiveObject))
                return false; //Do not register a reactive object several times.
            reactiveObjects.Add(reactiveObject, reactiveObject.ReactiveObjectId);
            return true;
        }

        internal void Add(Connection connection, ReactiveObject reactiveObject)
        {
            if (!Add(reactiveObject))
                return;

            /**
             *  Since we did serialize and de-Serialize the other connections
             *  reactiveObject, the properties are the same. But since this is
             *  not valid (there is only one owner) we have to correct some.
            **/

            reactiveObject.IsOwner = false;                                                             //We are not the owner of this object.
            reactiveObject.SilentAddSyncConnection(connection);
        }

        internal void Remove(ReactiveObject reactiveObject)
        {
            if (!reactiveObjects.ContainsKeyA(reactiveObject))
                return; //Do not un-register a reactive object several times.
            reactiveObjects.Remove(reactiveObject);
        }
    }
}
