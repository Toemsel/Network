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

using Network.Interfaces;
using Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Network.Utilities
{
    /// <summary>
    /// Maps individual <see cref="Packet"/>s to their unique ID value, so that
    /// they can be sent across the network and then deserialised. Also maps the
    /// <see cref="Packet"/>s IDs to the relevant <see cref="PacketReceivedHandler{P}"/>,
    /// should one be registered for that packet.
    /// </summary>
    public class PacketHandlerMap
    {
        #region Variables

        /// <summary>
        /// Maps each packet type to a dictionary containing registered objects
        /// which want to receive packets (i.e. Handlers) of the given type and
        /// their individual IDs.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<object, int>>
            packetTypeToHandlerIdMap = new Dictionary<Type, Dictionary<object, int>>();

        /// <summary>
        /// Maps each packet id to a tuple, holding the packet handler method
        /// and the object on which the handler should be called.
        /// </summary>
        private readonly Dictionary<int, (Delegate handlerDelegate, object handlerInstance)>
            packetIdToDelegateMethodMap = new Dictionary<int, (Delegate, object)>();

        /// <summary>
        /// Maps a <see cref="string"/> key to a <see cref="RawData"/> packet
        /// handler delegate method.
        /// </summary>
        private readonly Dictionary<string, Delegate> keyToDelegateMethodMap =
            new Dictionary<string, Delegate>();

        #endregion Variables

        #region Methods

        /// <summary>
        /// Checks whether the given packet has a registered handler method.
        /// </summary>
        /// <param name="packet">
        /// The packet for which to search for handler delegate methods.
        /// </param>
        /// <returns>
        /// Whether any delegate methods have been registered for the packet.
        /// </returns>
        public bool HasRegisteredHandler(Packet packet)
        {
            return packetIdToDelegateMethodMap.ContainsKey(packet.ID);
        }

        /// <summary>
        /// Restores the <see cref="PacketHandlerMap"/> to the state of the
        /// given packet handler map.
        /// </summary>
        /// <param name="map">
        /// The <see cref="PacketHandlerMap"/> whose state to restore to.
        /// </param>
        public void Restore(PacketHandlerMap map)
        {
            Type[] internalAssemblyTypes =
                Assembly.GetAssembly(typeof(PacketHandlerMap)).GetTypes();

            IEnumerable<Type> externalTypes =
                map.packetTypeToHandlerIdMap.Keys.ToList()
                    .Where(e => internalAssemblyTypes.All(i => i != e));

            foreach (Type currentExternalType in externalTypes)
            {
                if (!packetTypeToHandlerIdMap.ContainsKey(currentExternalType))
                    packetTypeToHandlerIdMap.Add(currentExternalType, map.packetTypeToHandlerIdMap[currentExternalType]);

                var externalIds = map.packetTypeToHandlerIdMap[currentExternalType].Values.ToArray();

                foreach (int currentExternalId in externalIds)
                {
                    if (!packetIdToDelegateMethodMap.ContainsKey(currentExternalId))
                        packetIdToDelegateMethodMap.Add(currentExternalId, map.packetIdToDelegateMethodMap[currentExternalId]);
                }
            }
        }

        #region Registering Packet Handlers

        /// <summary>
        /// Registers the given delegate method to be used for the given packet
        /// type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet for which the delegate method will be used.
        /// </typeparam>
        /// <param name="handlerDelegate">
        /// The delegate method to be invoked when the given packet is received.
        /// </param>
        /// <param name="handlerInstance">
        /// The handler object instance on which the delegate method will be invoked.
        /// </param>
        public void RegisterPacketHandler<P>(
            Delegate handlerDelegate, object handlerInstance) where P : Packet
        {
            Type packetType = typeof(P);

            if (packetTypeToHandlerIdMap.ContainsKey(packetType))
            {
                if (packetTypeToHandlerIdMap[packetType].ContainsKey(handlerInstance))
                {
                    // ignore, as we already have a handler. dont register a
                    // handler method more than once
                    return;
                }
            }
            else
            {
                // we havent seen this type before, so add it to the dict
                packetTypeToHandlerIdMap.Add(packetType, new Dictionary<object, int>());
            }

            packetTypeToHandlerIdMap[packetType].Add(
                handlerInstance, UidGenerator.GenerateUid<int>());

            packetIdToDelegateMethodMap.Add(
                UidGenerator.LastGeneratedUid<int>(),
                (handlerDelegate, handlerInstance));
        }

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{P}"/> method
        /// to be used for the given packet type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet for which the delegate method will be used.
        /// </typeparam>
        /// <param name="handlerDelegate">
        /// The delegate method to be invoked when the given packet is received.
        /// </param>
        /// <param name="handlerInstance">
        /// The handler object instance on which the delegate method will be invoked.
        /// </param>
        public void RegisterPacketDelegate<P>(
            PacketReceivedHandler<P> handlerDelegate,
            object handlerInstance) where P : Packet
        {
            RegisterPacketHandler<P>((Delegate)handlerDelegate, handlerInstance);
        }

        /// <summary>
        /// Registers the given static delegate method to be used for all
        /// packets of the given packet type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet for which the delegate method will be used.
        /// </typeparam>
        /// <param name="handlerDelegate">
        /// The static delegate method to be invoked when the given packet is
        /// received.
        /// </param>
        public void RegisterStaticPacketHandler<P>(Delegate handlerDelegate)
            where P : Packet
        {
            RegisterPacketHandler<P>(handlerDelegate, new object());
        }

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{P}"/> method
        /// to be used for all packets of the given packet type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet for which the delegate method will be used.
        /// </typeparam>
        /// <param name="handlerDelegate">
        /// The static delegate method to be invoked when the given packet is
        /// received.
        /// </param>
        public void RegisterStaticPacketHandler<P>(
            PacketReceivedHandler<P> handlerDelegate) where P : Packet
        {
            RegisterStaticPacketHandler<P>((Delegate)handlerDelegate);
        }

        /// <summary>
        /// Registers the given <see cref="Delegate"/> method to be used for
        /// all <see cref="RawData"/> packets that arrive with the given key.
        /// </summary>
        /// <param name="key">
        /// The key that identifies the primitive type.
        /// </param>
        /// <param name="handlerDelegate">
        /// The delegate method to invoke for incoming <see cref="RawData"/>
        /// packets with the given key.
        /// </param>
        public void RegisterStaticRawDataHandler(string key, Delegate handlerDelegate)
        {
            if (keyToDelegateMethodMap.ContainsKey(key))
            {
                // there already exists a handler for the given primitive key
                return;
            }

            keyToDelegateMethodMap[key] = handlerDelegate;
        }

        #endregion Registering Packet Handlers

        #region Deregistering Packet Handlers

        /// <summary>
        /// Deregisters packet handlers for the given packet type, on the given
        /// packet handler instance.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet for which to deregister any packet handlers.
        /// </typeparam>
        /// <param name="handlerInstance">
        /// The handler instance for which to deregisters packet handlers.
        /// </param>
        public void DeregisterPacketHandler<P>(object handlerInstance) where P : Packet
        {
            Type packetType = typeof(P);

            if ((!packetTypeToHandlerIdMap.ContainsKey(packetType) ||
                handlerInstance != null) &&
                 !packetTypeToHandlerIdMap[packetType].ContainsKey(handlerInstance))
            {
                // there are no packet handlers to deregister
                return;
            }

            // static methods
            if (handlerInstance == null && packetType.IsSubclassOf(typeof(RequestPacket)))
            {
                int temporaryPacketID =
                    packetTypeToHandlerIdMap[packetType].Values.ToList<int>()[0];
                packetIdToDelegateMethodMap.Remove(temporaryPacketID);
                packetTypeToHandlerIdMap[packetType].Clear();
                return;
            }

            // regular methods
            int packetID = packetTypeToHandlerIdMap[packetType][handlerInstance];
            packetTypeToHandlerIdMap[packetType].Remove(handlerInstance);

            if (packetTypeToHandlerIdMap[packetType].Count == 0)
            {
                packetTypeToHandlerIdMap.Remove(packetType);
            }

            packetIdToDelegateMethodMap.Remove(packetID);
        }

        /// <summary>
        /// Deregisters all static packet handlers for the given packet type.
        /// </summary>
        /// <typeparam name="P">
        /// The packet type for which to deregister all packet handlers.
        /// </typeparam>
        public void DeregisterStaticPacketHandler<P>() where P : Packet
        {
            DeregisterPacketHandler<P>(null);
        }

        /// <summary>
        /// Deregisters all static <see cref="RawData"/> packet handlers for
        /// the given key.
        /// </summary>
        /// <param name="key">
        /// The key for which to deregister packet handlers.
        /// </param>
        public void DeregisterStaticRawDataHandler(string key)
        {
            if (!keyToDelegateMethodMap.ContainsKey(key))
            {
                // no delegates for the given key exist
                return;
            }

            keyToDelegateMethodMap.Remove(key);
        }

        #endregion Deregistering Packet Handlers

        #endregion Methods

        #region Indexers

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles <see cref="RawData"/>
        /// packets for the primitive type identified by the given key.
        /// </summary>
        /// <param name="key">
        /// The key for whose primitive type to get a handler delegate.
        /// </param>
        /// <returns>
        /// The handler delegate associated with the given key.
        /// </returns>
        public Delegate this[string key]
        {
            get { return keyToDelegateMethodMap[key]; }
        }

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles packets with
        /// the given ID.
        /// </summary>
        /// <param name="packetID">
        /// The ID of the packet whose handler delegate to return.
        /// </param>
        /// <returns>
        /// The handler delegate associated with packets of the given id.
        /// </returns>
        public Delegate this[int packetID]
        {
            get { return packetIdToDelegateMethodMap[packetID].handlerDelegate; }
        }

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles packets of
        /// the given <see cref="Type"/>.
        /// </summary>
        /// <param name="packetType">
        /// The type of packet whose handler delegate to return.
        /// </param>
        /// <returns>
        /// The handler delegate registered for the given type.
        /// </returns>
        public Delegate this[Type packetType]
        {
            get
            {
                // there are registered packet handlers for the given type
                if (packetTypeToHandlerIdMap.ContainsKey(packetType))
                {
                    // gets the first ID of the handlers that are registered
                    // for the given packet type
                    int typeHandlerID =
                        packetTypeToHandlerIdMap[packetType].Values.First();

                    if (packetIdToDelegateMethodMap.ContainsKey(typeHandlerID))
                    {
                        return packetIdToDelegateMethodMap[typeHandlerID].handlerDelegate;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the ID associated with the given packet type and handler
        /// instance.
        /// </summary>
        /// <param name="packetType">
        /// The packet type whose handler ID to return.
        /// </param>
        /// <param name="handlerInstance">
        /// The handler whose ID to return.
        /// </param>
        /// <returns>
        /// The ID associated with the given handler of the given packet type.
        /// </returns>
        public int this[Type packetType, object handlerInstance]
        {
            get { return packetTypeToHandlerIdMap[packetType][handlerInstance]; }
        }

        #endregion Indexers
    }
}