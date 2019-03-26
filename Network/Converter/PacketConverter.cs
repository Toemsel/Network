#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-24-2015
//
// Last Modified By : Thomas
// Last Modified On : 28-09-2016
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2016
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
using Network.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Network.Converter
{
    /// <summary>
    /// Implements <see cref="IPacketConverter"/>, and provides methods to serialise
    /// and deserialise a packet to and from its binary form.
    /// </summary>
    public class PacketConverter : IPacketConverter
    {
        #region Variables

        /// <summary>
        /// Caches packet <see cref="Type"/>s and their relevant
        /// <see cref="PropertyInfo"/>s, to avoid slow and unnecessary reflection.
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> packetPropertyCache =
            new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// An object to synchronise multi-threaded access to the
        /// <see cref="packetPropertyCache"/>.
        /// </summary>
        private readonly object packetPropertyCacheLock = new object();

        #endregion Variables

        #region Methods

        /// <summary>
        /// Returns an array of the <see cref="PropertyInfo"/>s that need to be
        /// serialised on the given <see cref="Type"/>. If the given <see cref="Type"/>
        /// has already been cached, it will use the cached <see cref="PropertyInfo"/>
        /// array, to save CPU time.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> whose serialisable properties to get.
        /// </param>
        /// <returns>
        /// An array of all <see cref="PropertyInfo"/>s that should be serialised.
        /// </returns>
        private PropertyInfo[] GetTypeProperties(Type type)
        {
            lock (packetPropertyCacheLock)
            {
                // cache the properties to serialise if we haven't already
                if (!packetPropertyCache.ContainsKey(type))
                {
                    packetPropertyCache[type] =
                        PacketConverterHelper.GetTypeProperties(type);
                }

                return packetPropertyCache[type];
            }
        }

        #region Implementation of IPacketConverter

        /// <inheritdoc />
        public byte[] SerialisePacket(Packet packet)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            SerialiseObjectToWriter(packet, binaryWriter);

            return memoryStream.ToArray();
        }

        /// <inheritdoc />
        public byte[] SerialisePacket<P>(P packet) where P : Packet
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            SerialiseObjectToWriter(packet, binaryWriter);

            return memoryStream.ToArray();
        }

        /// <inheritdoc />
        public Packet DeserialisePacket(Type packetType, byte[] serialisedPacket)
        {
            MemoryStream memoryStream =
                new MemoryStream(serialisedPacket, 0, serialisedPacket.Length);

            BinaryReader binaryReader =
                new BinaryReader(memoryStream);

            // temporary object whose properties will be set during deserialisation
            Packet packet = PacketConverterHelper.InstantiatePacket(packetType);

            DeserialiseObjectFromReader(packet, binaryReader);

            return packet;
        }

        /// <inheritdoc />
        public P DeserialisePacket<P>(byte[] serialisedPacket) where P : Packet
        {
            MemoryStream memoryStream =
                new MemoryStream(serialisedPacket, 0, serialisedPacket.Length);

            BinaryReader binaryReader =
                new BinaryReader(memoryStream);

            // temporary object whose properties will be set during deserialisation
            P packet = PacketConverterHelper.InstantiateGenericPacket<P>();

            DeserialiseObjectFromReader(packet, binaryReader);

            return packet;
        }

        #endregion Implementation of IPacketConverter

        #region Serialisation

        /// <summary>
        /// Serialises all the properties on the given <see cref="object"/> that
        /// need to be serialised to the given <see cref="BinaryWriter"/>s
        /// underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose properties to serialise using the given
        /// <see cref="BinaryWriter"/>.
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/>
        /// to serialise the properties of the given <see cref="object"/>.
        /// </param>
        /// <remarks>
        /// This method can only serialise properties that lack the custom
        /// <see cref="PacketIgnorePropertyAttribute"/>.
        /// </remarks>
        private void SerialiseObjectToWriter(object obj, BinaryWriter binaryWriter)
        {
            PropertyInfo[] propertiesToSerialise = GetTypeProperties(obj.GetType());

            for (int i = 0; i < propertiesToSerialise.Length; ++i)
            {
                SerialiseObjectToWriter(obj, propertiesToSerialise[i], binaryWriter);
            }
        }

        /// <summary>
        /// Serialises the given <see cref="PropertyInfo"/> to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose <see cref="PropertyInfo"/> value to
        /// serialise.
        /// </param>
        /// <param name="propertyInfo">
        /// The <see cref="PropertyInfo"/> to serialise to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/>
        /// to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        private void SerialiseObjectToWriter(
            object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type propertyType = propertyInfo.PropertyType;
            dynamic propertyValue = propertyInfo.GetValue(obj);

            // we have an enumeration
            if (propertyType.IsEnum)
            {
                binaryWriter.Write(propertyValue.ToString());
            }
            // we have an array
            else if (propertyType.IsArray)
            {
                SerialiseArrayToWriter(obj, propertyInfo, binaryWriter);
            }
            // we have a list
            else if (propertyType.IsGenericType &&
                     propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerialiseListToWriter(obj, propertyInfo, binaryWriter);
            }
            // we have a non-primitive type
            else if (!PacketConverterHelper.TypeIsPrimitive(propertyType))
            {
                if (propertyValue != null) // not null non-primitive type value
                {
                    // there is a value to read from the network stream
                    binaryWriter.Write((byte)NetworkObjectState.NotNull);
                    SerialiseObjectToWriter(propertyValue, binaryWriter);
                }
                else // null non-primitive type value
                {
                    // there isn't a value to read from the network stream
                    binaryWriter.Write((byte)NetworkObjectState.Null);
                }
            }
            // we have a primitive type
            else
            {
                if (propertyValue != null) // not null primitive type value
                {
                    // there is a value to read from the network stream
                    binaryWriter.Write((byte)NetworkObjectState.NotNull);
                    SerialiseObjectToWriter(propertyValue, binaryWriter);
                }
                else // null primitive type value
                {
                    // there isn't a value to read from the network stream
                    binaryWriter.Write((byte)NetworkObjectState.Null);
                }
            }
        }

        /// <summary>
        /// Serialises the given <see cref="Array"/> to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="SerialiseObjectToWriter(object,BinaryWriter)"/>
        /// to serialise each of the <see cref="Array"/>s elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="Array"/> to serialise to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// The <see cref="PropertyInfo"/> holding the <see cref="Array"/>.
        /// <param name="propertyInfo">
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/>
        /// to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="Array"/> held in the given <see cref="PropertyInfo"/>
        /// is null, or if the <see cref="Array"/>s elements do not have a type.
        /// </exception>
        private void SerialiseArrayToWriter(
            object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type elementType = propertyInfo.PropertyType.GetElementType();
            Array array = (Array)propertyInfo.GetValue(obj);

            binaryWriter.Write(array?.Length ?? 0);

            if (elementType.IsClass &&
                !PacketConverterHelper.TypeIsPrimitive(elementType))
            {
                array
                    .GetEnumerator()
                    .ToList<object>()
                    .ForEach(element =>
                    {
                        SerialiseObjectToWriter(element, binaryWriter);
                    });
            }
            else // primitive type
            {
                array
                    .GetEnumerator()
                    .ToList<object>().ForEach(primitiveElement =>
                    {
                        dynamic primitiveValue = primitiveElement;
                        binaryWriter.Write(primitiveValue);
                    });
            }
        }

        /// <summary>
        /// Serialises the given <see cref="IList"/> to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="SerialiseObjectToWriter(object,BinaryWriter)"/>
        /// to serialise each of the <see cref="IList"/>s elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="IList"/> to serialise to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// The <see cref="PropertyInfo"/> holding the <see cref="IList"/>.
        /// <param name="propertyInfo">
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/>
        /// to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="IList"/> held in the given <see cref="PropertyInfo"/>
        /// is null, or if the <see cref="IList"/>s elements do not have a type.
        /// </exception>
        private void SerialiseListToWriter(
            object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type elementType = propertyInfo
                .PropertyType
                .GetGenericArguments()[0];

            IList list = (IList)Activator
                .CreateInstance(typeof(List<>)
                    .MakeGenericType(elementType));

            ((IEnumerable)propertyInfo.GetValue(obj))
                ?.GetEnumerator()
                .ToList<object>()
                .ForEach(o => list.Add(o));

            binaryWriter.Write(list.Count);

            if (elementType.IsClass &&
                !PacketConverterHelper.TypeIsPrimitive(elementType))
            {
                list
                    .GetEnumerator()
                    .ToList<object>()
                    .ForEach(element =>
                    {
                        SerialiseObjectToWriter(element, binaryWriter);
                    });
            }
            else // primitive type
            {
                list
                    .GetEnumerator()
                    .ToList<object>()
                    .ForEach(primitiveElement =>
                    {
                        dynamic primitiveValue = primitiveElement;
                        binaryWriter.Write(primitiveValue);
                    });
            }
        }

        #endregion Serialisation

        #region Deserialisation

        /// <summary>
        /// Deserialises all the properties on the given <see cref="object"/> that
        /// can be deserialised from the given <see cref="BinaryReader"/>s
        /// underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose properties to deserialise using the given
        /// <see cref="BinaryReader"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/>
        /// to deserialise the properties of the given <see cref="object"/>.
        /// </param>
        /// <returns>
        /// The given <see cref="object"/> with all deserialisable properties
        /// set.
        /// </returns>
        /// <remarks>
        /// This method can only deserialise properties that lack the custom
        /// <see cref="PacketIgnorePropertyAttribute"/>. Any other properties
        /// will be left at their default values.
        /// </remarks>
        private object DeserialiseObjectFromReader(object obj, BinaryReader binaryReader)
        {
            PropertyInfo[] propertiesToSerialise = GetTypeProperties(obj.GetType());

            for (int i = 0; i < propertiesToSerialise.Length; ++i)
            {
                propertiesToSerialise[i].SetValue(
                    obj,
                    DeserialiseObjectFromReader(
                        obj, propertiesToSerialise[i], binaryReader));
            }

            return obj;
        }

        /// <summary>
        /// Deserialises the given <see cref="PropertyInfo"/> from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose <see cref="PropertyInfo"/> value to
        /// deserialise.
        /// </param>
        /// <param name="propertyInfo">
        /// The <see cref="PropertyInfo"/> to deserialise from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> frpm whose underlying <see cref="MemoryStream"/>
        /// to deserialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <returns>
        /// The <see cref="object"/> deserialised from the <see cref="MemoryStream"/>.
        /// This can be null if the <see cref="NetworkObjectState"/> is
        /// <see cref="NetworkObjectState.Null"/>.
        /// </returns>
        private object DeserialiseObjectFromReader(
            object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            Type propertyType = propertyInfo.PropertyType;

            // we have an enumeration
            if (propertyType.IsEnum)
            {
                return Enum.Parse(propertyType, binaryReader.ReadString());
            }

            // we have an array
            if (propertyType.IsArray)
            {
                return ReadArrayFromStream(obj, propertyInfo, binaryReader);
            }

            // we have a generic list
            if (propertyType.IsGenericType &&
                     propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ReadListFromStream(obj, propertyInfo, binaryReader);
            }

            // we have a non-primitive type
            if (!PacketConverterHelper.TypeIsPrimitive(propertyType))
            {
                NetworkObjectState networkObjectState =
                    (NetworkObjectState)binaryReader.ReadByte();

                if (networkObjectState == NetworkObjectState.NotNull)
                {
                    // this will recursively deserialise all the properties
                    // that are made up of primitives (even complex types)
                    return DeserialiseObjectFromReader(
                        PacketConverterHelper.InstantiateObject(propertyType), binaryReader);
                }

                // if it is null we just return null
                return null;
            }

            // we have a primitive type
            return ReadPrimitiveFromStream(propertyInfo, binaryReader);
        }

        /// <summary>
        /// Deserialises the given <see cref="Array"/> from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="DeserialiseObjectFromReader(object,BinaryReader)"/>
        /// to serialise each of the <see cref="Array"/>s elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="Array"/> to deserialise from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// The <see cref="PropertyInfo"/> holding the <see cref="Array"/>.
        /// <param name="propertyInfo">
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/>
        /// to deserialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <see cref="Array"/>s elements do not have a type.
        /// </exception>
        private Array ReadArrayFromStream(
            object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int arraySize = binaryReader.ReadInt32();

            Type elementType = propertyInfo.PropertyType.GetElementType();
            Array array = Array.CreateInstance(elementType, arraySize);

            for (int i = 0; i < arraySize; ++i)
            {
                if (elementType.IsClass && !PacketConverterHelper.TypeIsPrimitive(elementType))
                {
                    array.SetValue(
                        DeserialiseObjectFromReader(
                            PacketConverterHelper.InstantiateObject(elementType), binaryReader), i);
                }
                else
                {
                    array.SetValue(
                        ReadPrimitiveFromStream(elementType, binaryReader), i);
                }
            }

            return array;
        }

        /// <summary>
        /// Deserialises the given <see cref="IList"/> from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="DeserialiseObjectFromReader(object,BinaryReader)"/>
        /// to serialise each of the <see cref="IList"/>s elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="IList"/> to deserialise from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// The <see cref="PropertyInfo"/> holding the <see cref="IList"/>.
        /// <param name="propertyInfo">
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/>
        /// to deserialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="IList"/> held in the <see cref="MemoryStream"/>
        /// is null, or if the <see cref="IList"/>s elements do not have a type.
        /// </exception>
        private IList ReadListFromStream(
            object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int listSize = binaryReader.ReadInt32();

            Type listType = propertyInfo.PropertyType.GetGenericArguments()[0];

            IList list = (IList)Activator
                .CreateInstance(typeof(List<>).MakeGenericType(listType));

            for (int i = 0; i < listSize; ++i)
            {
                if (listType.IsClass && !PacketConverterHelper.TypeIsPrimitive(listType))
                {
                    list.Add(
                        DeserialiseObjectFromReader(
                            PacketConverterHelper.InstantiateObject(listType), binaryReader));
                }
                else
                {
                    list.Add(ReadPrimitiveFromStream(listType, binaryReader));
                }
            }

            return list;
        }

        /// <summary>
        /// Deserialises and returns a primitive object from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="propertyInfo">
        /// The <see cref="PropertyInfo"/> to deserialise from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/>
        /// to deserialise the primitive.
        /// </param>
        /// <returns>
        /// The primitive object that was deserialised from the <see cref="MemoryStream"/>.
        /// </returns>
        /// <remarks>
        /// This method can return 'null' if the <see cref="NetworkObjectState"/>
        /// is <see cref="NetworkObjectState.Null"/>.
        /// </remarks>
        private object ReadPrimitiveFromStream(
            PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            NetworkObjectState networkObjectState =
                (NetworkObjectState)binaryReader.ReadByte();

            if (networkObjectState == NetworkObjectState.NotNull)
            {
                return ReadPrimitiveFromStream(
                    propertyInfo.PropertyType, binaryReader);
            }

            return null;
        }

        /// <summary>
        /// Reads a primitive type from the given <see cref="BinaryReader"/>s
        /// underlying <see cref="MemoryStream"/> and returns it.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> of the primitive to read from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/>
        /// to read the primitive.
        /// </param>
        /// <returns>
        /// The primitive that was read from the given <see cref="BinaryReader"/>s
        /// underlying <see cref="MemoryStream"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown whenever a <see cref="Type"/> is passed to this method that
        /// is not a primitive.
        /// </exception>
        private object ReadPrimitiveFromStream(Type type, BinaryReader binaryReader)
        {
            #region Reading Primitives From Stream

            if (type == typeof(String))
            {
                return binaryReader.ReadString();
            }

            if (type == typeof(Int16))
            {
                return binaryReader.ReadInt16();
            }

            if (type == typeof(Int32))
            {
                return binaryReader.ReadInt32();
            }

            if (type == typeof(Int64))
            {
                return binaryReader.ReadInt64();
            }

            if (type == typeof(Boolean))
            {
                return binaryReader.ReadBoolean();
            }

            if (type == typeof(Byte))
            {
                return binaryReader.ReadByte();
            }

            if (type == typeof(Char))
            {
                return binaryReader.ReadChar();
            }

            if (type == typeof(Decimal))
            {
                return binaryReader.ReadDecimal();
            }

            if (type == typeof(Double))
            {
                return binaryReader.ReadDouble();
            }

            if (type == typeof(Single))
            {
                return binaryReader.ReadSingle();
            }

            if (type == typeof(UInt16))
            {
                return binaryReader.ReadUInt16();
            }

            if (type == typeof(UInt32))
            {
                return binaryReader.ReadUInt32();
            }

            if (type == typeof(UInt64))
            {
                return binaryReader.ReadUInt64();
            }

            #endregion Reading Primitives From Stream

            // If we reached here then we were not given a primitive type.
            // This is not supported.
            throw new NotSupportedException();
        }

        #endregion Deserialisation

        #endregion Methods
    }
}