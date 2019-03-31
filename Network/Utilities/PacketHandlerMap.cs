using Network.Interfaces;
using Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Network.Utilities
{
    /// <summary>
    /// Maps individual <see cref="Packet"/>s to their unique ID value, so that they can be sent across the network and
    /// then deserialised. Also maps the <see cref="Packet"/>s IDs to the relevant <see cref="PacketReceivedHandler{P}"/>,
    /// should one be registered for that packet.
    /// </summary>
    public class PacketHandlerMap
    {
        #region Variables

        /// <summary>
        /// Maps each packet type to a dictionary containing registered objects which want to receive packets
        /// (i.e. Handlers) of the given type and their individual IDs.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<object, int>> packetTypeToHandlerIdMap =
            new Dictionary<Type, Dictionary<object, int>>();

        /// <summary>
        /// Maps each packet id to a tuple, holding the packet handler method and the object on which the handler should be called.
        /// </summary>
        private readonly Dictionary<int, (Delegate handlerDelegate, object handlerInstance)> packetIdToDelegateMethodMap =
            new Dictionary<int, (Delegate, object)>();

        /// <summary>
        /// Maps a <see cref="string"/> key to a <see cref="RawData"/> packet handler delegate method.
        /// </summary>
        private readonly Dictionary<string, Delegate> keyToDelegateMethodMap = new Dictionary<string, Delegate>();

        #endregion Variables

        #region Methods

        /// <summary>
        /// Checks whether the given packet has a registered handler method.
        /// </summary>
        /// <param name="packet">The packet for which to search for handler delegate methods.</param>
        /// <returns>Whether any delegate methods have been registered for the packet.</returns>
        internal bool HasRegisteredHandler(Packet packet)
        {
            return packetIdToDelegateMethodMap.ContainsKey(packet.ID);
        }

        /// <summary>
        /// Restores the <see cref="PacketHandlerMap"/> to the state of the given packet handler map.
        /// </summary>
        /// <param name="map">The <see cref="PacketHandlerMap"/> whose state to restore to.</param>
        internal void Restore(PacketHandlerMap map)
        {
            Type[] internalAssemblyTypes =
                Assembly.GetAssembly(typeof(PacketHandlerMap)).GetTypes();

            IEnumerable<Type> externalTypes =
                map.packetTypeToHandlerIdMap.Keys.ToList().Where(e => internalAssemblyTypes.All(i => i != e));

            foreach (Type currentExternalType in externalTypes)
            {
                if (!packetTypeToHandlerIdMap.ContainsKey(currentExternalType))
                {
                    packetTypeToHandlerIdMap.Add(currentExternalType,
                        map.packetTypeToHandlerIdMap[currentExternalType]);
                }

                int[] externalIds = map.packetTypeToHandlerIdMap[currentExternalType].Values.ToArray();

                foreach (int currentExternalId in externalIds)
                {
                    if (!packetIdToDelegateMethodMap.ContainsKey(currentExternalId))
                    {
                        packetIdToDelegateMethodMap.Add(currentExternalId,
                            map.packetIdToDelegateMethodMap[currentExternalId]);
                    }
                }
            }
        }

        #region Registering Packet Handlers

        /// <summary>
        /// Registers the given delegate method to be used for the given packet type.
        /// </summary>
        /// <typeparam name="P">The type of packet for which the delegate method will be used.</typeparam>
        /// <param name="handlerDelegate">The delegate method to be invoked when the given packet is received.</param>
        /// <param name="handlerInstance">The handler object instance on which the delegate method will be invoked.</param>
        internal void RegisterPacketHandler<P>(Delegate handlerDelegate, object handlerInstance) where P : Packet
        {
            Type packetType = typeof(P);

            if (packetTypeToHandlerIdMap.ContainsKey(packetType))
            {
                if (packetTypeToHandlerIdMap[packetType].ContainsKey(handlerInstance))
                {
                    //Ignore, as we already have a handler. Don't register a handler method more than once
                    return;
                }
            }
            else
            {
                //We haven't seen this type before, so add it to the dict
                packetTypeToHandlerIdMap.Add(packetType, new Dictionary<object, int>());
            }

            packetTypeToHandlerIdMap[packetType].Add(handlerInstance, UidGenerator.GenerateUid<int>());

            packetIdToDelegateMethodMap.Add(UidGenerator.LastGeneratedUid<int>(), (handlerDelegate, handlerInstance));
        }

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{P}"/> method to be used for the given packet type.
        /// </summary>
        /// <typeparam name="P">The type of packet for which the delegate method will be used.</typeparam>
        /// <param name="handlerDelegate">The delegate method to be invoked when the given packet is received.</param>
        /// <param name="handlerInstance">The handler object instance on which the delegate method will be invoked.</param>
        internal void RegisterPacketDelegate<P>(PacketReceivedHandler<P> handlerDelegate, object handlerInstance) where P : Packet
        {
            RegisterPacketHandler<P>((Delegate)handlerDelegate, handlerInstance);
        }

        /// <summary>
        /// Registers the given static delegate method to be used for all packets of the given packet type.
        /// </summary>
        /// <typeparam name="P">The type of packet for which the delegate method will be used.</typeparam>
        /// <param name="handlerDelegate">The static delegate method to be invoked when the given packet is received.
        /// </param>
        internal void RegisterStaticPacketHandler<P>(Delegate handlerDelegate) where P : Packet
        {
            RegisterPacketHandler<P>(handlerDelegate, new object());
        }

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{P}"/> method to be used for all packets of the given packet type.
        /// </summary>
        /// <typeparam name="P">The type of packet for which the delegate method will be used.</typeparam>
        /// <param name="handlerDelegate">The static delegate method to be invoked when the given packet is received.</param>
        internal void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handlerDelegate) where P : Packet
        {
            RegisterStaticPacketHandler<P>((Delegate)handlerDelegate);
        }

        /// <summary>
        /// Registers the given <see cref="Delegate"/> method to be used for all <see cref="RawData"/> packets that arrive with the given key.
        /// </summary>
        /// <param name="key">The key that identifies the primitive type.</param>
        /// <param name="handlerDelegate">The delegate method to invoke for incoming <see cref="RawData"/> packets with the given key.</param>
        internal void RegisterStaticRawDataHandler(string key, Delegate handlerDelegate)
        {
            if (keyToDelegateMethodMap.ContainsKey(key))
            {
                //There already exists a handler for the given primitive key
                return;
            }

            keyToDelegateMethodMap[key] = handlerDelegate;
        }

        #endregion Registering Packet Handlers

        #region Deregistering Packet Handlers

        /// <summary>
        /// Deregisters packet handlers for the given packet type, on the given packet handler instance.
        /// </summary>
        /// <typeparam name="P">The type of packet for which to deregister any packet handlers.</typeparam>
        /// <param name="handlerInstance">The handler instance for which to deregisters packet handlers.</param>
        internal void DeregisterPacketHandler<P>(object handlerInstance) where P : Packet
        {
            Type packetType = typeof(P);

            if ((!packetTypeToHandlerIdMap.ContainsKey(packetType) || handlerInstance != null) && !packetTypeToHandlerIdMap[packetType].ContainsKey(handlerInstance))
            {
                //There are no packet handlers to deregister
                return;
            }

            //Static methods
            if (handlerInstance == null && packetType.IsSubclassOf(typeof(RequestPacket)))
            {
                int temporaryPacketId = packetTypeToHandlerIdMap[packetType].Values.ToList()[0];
                packetIdToDelegateMethodMap.Remove(temporaryPacketId);
                packetTypeToHandlerIdMap[packetType].Clear();
                return;
            }

            //Regular methods
            int packetId = packetTypeToHandlerIdMap[packetType][handlerInstance];
            packetTypeToHandlerIdMap[packetType].Remove(handlerInstance);

            if (packetTypeToHandlerIdMap[packetType].Count == 0)
            {
                packetTypeToHandlerIdMap.Remove(packetType);
            }

            packetIdToDelegateMethodMap.Remove(packetId);
        }

        /// <summary>
        /// Deregisters all static packet handlers for the given packet type.
        /// </summary>
        /// <typeparam name="P">The packet type for which to deregister all packet handlers.</typeparam>
        internal void DeregisterStaticPacketHandler<P>() where P : Packet
        {
            DeregisterPacketHandler<P>(null);
        }

        /// <summary>
        /// Deregisters all static <see cref="RawData"/> packet handlers for the given key.
        /// </summary>
        /// <param name="key">The key for which to deregister packet handlers.</param>
        internal void DeregisterStaticRawDataHandler(string key)
        {
            if (!keyToDelegateMethodMap.ContainsKey(key))
            {
                //No delegates for the given key exist
                return;
            }

            keyToDelegateMethodMap.Remove(key);
        }

        #endregion Deregistering Packet Handlers

        #endregion Methods

        #region Indexers

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles <see cref="RawData"/> packets for the primitive type
        /// identified by the given key.
        /// </summary>
        /// <param name="key">The key for whose primitive type to get a handler delegate.</param>
        /// <returns>The handler delegate associated with the given key.</returns>
        internal Delegate this[string key]
        {
            get { return keyToDelegateMethodMap[key]; }
        }

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles packets with the given ID.
        /// </summary>
        /// <param name="packetId">The ID of the packet whose handler delegate to return.</param>
        /// <returns>The handler delegate associated with packets of the given id.</returns>
        internal Delegate this[int packetId]
        {
            get { return packetIdToDelegateMethodMap[packetId].handlerDelegate; }
        }

        /// <summary>
        /// Gets the <see cref="Delegate"/> method which handles packets of the given <see cref="Type"/>.
        /// </summary>
        /// <param name="packetType">The type of packet whose handler delegate to return.</param>
        /// <returns>The handler delegate registered for the given type.</returns>
        internal Delegate this[Type packetType]
        {
            get
            {
                //There are no registered packet handlers for the given type
                if (!packetTypeToHandlerIdMap.ContainsKey(packetType))
                    return null;

                //Gets the first ID of the handlers that are registered for the given packet type
                int typeHandlerId =
                    packetTypeToHandlerIdMap[packetType].Values.First();

                return packetIdToDelegateMethodMap.ContainsKey(typeHandlerId)
                    ? packetIdToDelegateMethodMap[typeHandlerId].handlerDelegate
                    : null;
            }
        }

        /// <summary>
        /// Gets the ID associated with the given packet type and handler instance.
        /// </summary>
        /// <param name="packetType">The packet type whose handler ID to return.</param>
        /// <param name="handlerInstance">The handler whose ID to return.</param>
        /// <returns>The ID associated with the given handler of the given packet type.</returns>
        internal int this[Type packetType, object handlerInstance]
        {
            get { return packetTypeToHandlerIdMap[packetType][handlerInstance]; }
        }

        #endregion Indexers
    }
}