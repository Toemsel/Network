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
using Network.Enums;
using Network.Packets;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Network.Reactive
{
    /// <summary>
    /// Builds the base for every reactive object.
    /// It's aim is to automatically synchronize object properties with
    /// the server and or client. Hence, exchanging data won't
    /// require sending manual packages anymore. In addition, both
    /// communication partners "share" the "same" object.
    /// </summary>
    public abstract class ReactiveObject : ReactivePacket, IDisposable
    {
        private ConcurrentBag<Connection> syncConnections = new ConcurrentBag<Connection>();
        private event Action<ReactiveObject> reactiveObjectRemovedEvent;
        private object setValueLockObject = new object();

        /// <summary>
        /// [True] if this object isn't linked anymore to other connections.
        /// [False] if this object still is able to receive and (depending on the configuration) send sync states.
        /// </summary>
        public bool IsDisposed { get; internal set; } = false;

        /// <summary>
        /// Indicated if this is the owner of the ReactiveObject.
        /// </summary>
        public bool IsOwner { get; internal set; } = true;

        /// <summary>
        /// The ID to identify this object across all other reactive objects.
        /// </summary>
        public byte[] ReactiveObjectId { get; internal set; } = Guid.NewGuid().ToByteArray();

        /// <summary>
        /// The direction the properties will be synced by default. (If no additional SyncDirectionAttribute is given)
        /// A SyncDirection change, after the ReactiveObject has been activated, has no effect on the other connections side.
        /// Only new connection will obtain the new value.
        /// </summary>
        public SyncDirection SyncDirection { get; set; } = SyncDirection.TwoWay;

        /// <summary>
        /// If the owner has removed the object (called RemoveReactiveObject)
        /// this object isn't linked anymore. Thus, modifications won't lead
        /// to any synchronisation. This object is flagged as "disposed" and
        /// shouldn't be used anymore.
        /// </summary>
        public event Action<ReactiveObject> RemovedEvent
        {
            add => reactiveObjectRemovedEvent += value;
            remove => reactiveObjectRemovedEvent -= value;
        }

        /// <summary>
        /// Adds a connection which should receive this object.
        /// This object instance and the given connection will automatically
        /// sync property changes.
        /// </summary>
        /// <param name="connection">The connection to sync with.</param>
        /// <param name="direction">The direction to sync this object.</param>
        public void AddSyncConnection(Connection connection)
        {
            connection.Reactive.Add(this);
            SilentAddSyncConnection(connection);
            connection.Send(new AddReactiveObject(this));
        }

        internal void SilentAddSyncConnection(Connection connection)
        {
            if (syncConnections.Contains(connection))
                return; //If the connection is already in the syncList, skip it.
            syncConnections.Add(connection);
        }

        /// <summary>
        /// Removes a connection to sync with. Once removed, the connection
        /// wont receive any updates, nor is able to push new values.
        /// </summary>
        /// <param name="connection"></param>
        public void RemoveSyncConnection(Connection connection)
        {
            if (!syncConnections.Contains(connection))
                return; //If the connection is not in the sync list, we cant remove it.

            while (!syncConnections.TryTake(out Connection con))
                Thread.Sleep(5);

            /*
             *  If we aren't the owner of this object, nor
             *  we are allowed to sync TwoWay, we do not have
             *  to permission to remove this reactiveObject.
            */

            if (!IsOwner && SyncDirection == SyncDirection.OneWay)
                return;

            connection.Reactive.Remove(this);
            //ToDo: Send that we are gone!
        }

        public bool Sync<T>(ref T obj, T val, bool forceRefresh = false, [CallerMemberName] string callerName = "")
        {
            //No need to synchronize if the objects have the same reference.
            if (val != null && obj != null && obj.Equals(val) && !forceRefresh)
                return false;

            obj = val;

            /**
             *  We are only allowed to sync the object's value iff:
             *  1. The property is basically allowed to sync. (IsAllowedToSync)
             *  2. The connection is allowed to sync. (IsConnectionAllowedToSync)
             *  3. It isn't active or already disposed.
            */

            if (IsDisposed || Monitor.IsEntered(setValueLockObject))
                return true;

            /**
             *  If the "val" type does not equal the "callerName" type, we can't send it
             *  over the network. (Receiver won't be able to set the value). This only
             *  happens if the user passes a wrong name for the property.
            **/

            if (ReactiveContainer.Singleton[GetType(), callerName]?.PropertyType != val?.GetType())
                return true;

            IsAllowedToSync(callerName).ContinueWith(isAllowedToSync =>
            {
                if(isAllowedToSync.Exception == null && isAllowedToSync.Result)
                    Reactive.ReactiveSyncRequest(this, val, callerName, syncConnections);
            });

            return true;
        }

        internal void SyncReceive(object val, string callerName)
        {
            /*
             *  SyncReceive is simpler than sync.
             *  If we receive a value, we already know:
             *  1. The property is Read/Write able.
             *  2. The property is not set on "ignore"
             *  3. SyncDirections or anything else do not apply.
            */

            Monitor.Enter(setValueLockObject);
            PropertyInfo propertyInfo = ReactiveContainer.Singleton[GetType(), callerName];
            object currentPropertyValue = propertyInfo.GetValue(this);

            BeforeValueReceive(currentPropertyValue, val, callerName);
            propertyInfo.SetValue(this, val);
            AfterValueReceive(currentPropertyValue, val, callerName);
            Monitor.Exit(setValueLockObject);
        }

        /// <summary>
        /// Is the given property allowed to be synced of the network.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private async Task<bool> IsAllowedToSync(string callerName)
        {
            PropertyInfo propertyInfo = ReactiveContainer.Singleton[GetType(), callerName];
            SyncAttribute syncAttribute = ReactiveContainer.Singleton[propertyInfo];

            /* 
             * The propertyInfo could be null, because the user wants to ignore the property.
             * (PacketIgnoreAttribute). Or, it is not null, but the user only wants to sync
             * it oneWay. (Even if the local value changes, we aren't allowed to sync it)
             * If we can't read or write the property, we also have to skip the sync.
            */

            if (propertyInfo == null || !propertyInfo.CanWrite || !propertyInfo.CanRead)
                return false;

            /**
             *  There is no property describing whether the property is allowed to be synced or not.
             *  In this case, the reactiveObject's SyncDirection will decide whether it will be syned.
             *  e.g.:   1. Obj is TwoWay and we have no PropertyAttribute.          ->  Send
             *          2. Obj is OneWay and we have no PropertyAttribute.          ->  !Send
             *          3. Obj is OneWay and we have PropertyAttribute Mode=TwoWay  ->  Send
             *          4. Obj is TwoWay and we have PropertyAttribute Mode=OneWay  ->  !Send
            **/

            if (syncAttribute == null)
                return SyncDirection == SyncDirection.TwoWay || IsOwner;

            /*
             *  We are allowed to sync if we are the owner OR
             *  TwoWay communication is enabled. This depends
             *  on the "SyncAttribute" which can be assigned to
             *  every property.
            */

            if (syncAttribute.Delay != 0)
                await Task.Delay(syncAttribute.Delay);

            return syncAttribute.Direction == SyncDirection.TwoWay || IsOwner;
        }

        public virtual void BeforeValueReceive(object oldValue, object newValue, string callerName) { }
        public virtual void AfterValueReceive(object oldValue, object newValue, string callerName) { }

        /// <summary>
        /// Removes all connections (no connection will receive any update,
        /// nor is able to push new values) and this reactive object will be
        /// flagged as "disposed". Hence, won't be able to sync anymore, even
        /// if a new connection has been added. In addition, all listening 
        /// connections will dump this object as well.
        /// </summary>
        public void RemoveReactiveObject()
        {
            //We can only dispose the object once.
            if (IsDisposed)
                return;

            syncConnections.ToList().ForEach(RemoveSyncConnection);
            reactiveObjectRemovedEvent?.Invoke(this);
            IsDisposed = true;
        }

        public void Dispose()
        {
            RemoveReactiveObject();
        }
    }
}