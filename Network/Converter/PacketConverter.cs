﻿using Network.Attributes;
using Network.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Network.Converter
{
    /// <summary>
    /// Implements <see cref="IPacketConverter"/>, and provides methods to serialise and deserialise a <see cref="Packet"/>
    /// object to and from its binary form.
    /// </summary>
    internal class PacketConverter : IPacketConverter
    {
        #region Variables

        /// <summary>
        /// Caches packet <see cref="Type"/>s and their relevant <see cref="PropertyInfo"/>s, to avoid slow and unnecessary reflection.
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> packetPropertyCache = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// An object to synchronise multi-threaded access to the <see cref="packetPropertyCache"/>.
        /// </summary>
        private readonly object packetPropertyCacheLock = new object();

        #endregion Variables

        #region Methods

        #region Implementation of IPacketConverter

        /// <inheritdoc />
        public byte[] GetBytes(Packet packet)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            SerialiseObjectToWriter(packet, binaryWriter);

            return memoryStream.ToArray();
        }

        /// <inheritdoc />
        public byte[] GetBytes<P>(P packet) where P : Packet
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            SerialiseObjectToWriter(packet, binaryWriter);

            return memoryStream.ToArray();
        }

        /// <inheritdoc />
        public Packet GetPacket(Type packetType, byte[] serialisedPacket)
        {
            MemoryStream memoryStream = new MemoryStream(serialisedPacket, 0, serialisedPacket.Length);
            BinaryReader binaryReader = new BinaryReader(memoryStream);

            //Temporary object whose properties will be set during deserialisation
            Packet packet = PacketConverterHelper.InstantiatePacket(packetType);

            DeserialiseObjectFromReader(packet, binaryReader);

            return packet;
        }

        /// <inheritdoc />
        public P GetPacket<P>(byte[] serialisedPacket) where P : Packet
        {
            MemoryStream memoryStream = new MemoryStream(serialisedPacket, 0, serialisedPacket.Length);
            BinaryReader binaryReader = new BinaryReader(memoryStream);

            //Temporary object whose properties will be set during deserialisation
            P packet = PacketConverterHelper.InstantiateGenericPacket<P>();

            DeserialiseObjectFromReader(packet, binaryReader);

            return packet;
        }

        #endregion Implementation of IPacketConverter

        /// <summary>
        /// Returns an array of the <see cref="PropertyInfo"/>s that need to be serialised on the given <see cref="Type"/>.
        /// If the given <see cref="Type"/> has already been cached, it will use the cached <see cref="PropertyInfo"/> array,
        /// to save CPU time.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> whose serialisable properties to get.</param>
        /// <returns>
        /// An array of all <see cref="PropertyInfo"/>s that should be serialised on the given <see cref="Type"/>
        /// </returns>
        private PropertyInfo[] GetTypeProperties(Type type)
        {
            lock (packetPropertyCacheLock)
            {
                //Cache the properties to serialise if we haven't already
                if (!packetPropertyCache.ContainsKey(type))
                {
                    packetPropertyCache[type] = PacketConverterHelper.GetTypeProperties(type);
                }

                return packetPropertyCache[type];
            }
        }

        #region Serialisation

        /// <summary>
        /// Serialises all the properties on the given <see cref="object"/> that need to be serialised to the given
        /// <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose properties to serialise using the given <see cref="BinaryWriter"/>.
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/> to serialise the properties of
        /// the given <see cref="object"/>.
        /// </param>
        /// <remarks>
        /// This method can only serialise properties that lack the custom <see cref="PacketIgnorePropertyAttribute"/>.
        /// </remarks>
        private void SerialiseObjectToWriter(object obj, BinaryWriter binaryWriter)
        {
            PropertyInfo[] propertiesToSerialise = GetTypeProperties(obj.GetType());

            for (int i = 0; i < propertiesToSerialise.Length; i++)
            {
                SerialiseObjectToWriter(obj, propertiesToSerialise[i], binaryWriter);
            }
        }

        /// <summary>
        /// Serialises the given <see cref="PropertyInfo"/> to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> whose <see cref="PropertyInfo"/> value to serialise.</param>
        /// <param name="propertyInfo">
        /// The <see cref="PropertyInfo"/> to serialise to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/> to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        private void SerialiseObjectToWriter(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type propertyType = propertyInfo.PropertyType;
            dynamic propertyValue = propertyInfo.GetValue(obj);

            //We have an enum
            if (propertyType.IsEnum)
            {
                binaryWriter.Write(propertyValue.ToString());
            }
            //We have an array
            else if (propertyType.IsArray)
            {
                SerialiseArrayToWriter(obj, propertyInfo, binaryWriter);
            }
            //We have a list
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerialiseListToWriter(obj, propertyInfo, binaryWriter);
            }
            //We have a non-primitive type
            else if (!PacketConverterHelper.TypeIsPrimitive(propertyType))
            {
                //If is an struct return so doesnt throw an exception
                if (propertyType.IsValueType)
                {
                    binaryWriter.Write((byte)ObjectState.NotNull);
                    SerialiseObjectToWriter(propertyValue, binaryWriter);
                    return;
                }
                if (propertyValue != null) //Not null non-primitive type value
                {
                    //There is a value to read from the network stream
                    binaryWriter.Write((byte)ObjectState.NotNull);
                    SerialiseObjectToWriter(propertyValue, binaryWriter);
                }
                else //Null non-primitive type value
                {
                    //There isn't a value to read from the network stream
                    binaryWriter.Write((byte)ObjectState.Null);
                }
            }
            //We have a primitive type
            else
            {
                if (propertyValue != null) //Not null primitive type value
                {
                    //There is a value to read from the network stream
                    binaryWriter.Write((byte)ObjectState.NotNull);
                    //We write the value to the stream
                    binaryWriter.Write(propertyValue);
                }
                else //Null primitive type value
                {
                    //There isn't a value to read from the network stream
                    binaryWriter.Write((byte)ObjectState.Null);
                }
            }
        }

        /// <summary>
        /// Serialises the given <see cref="Array"/> to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="SerialiseObjectToWriter(object,BinaryWriter)"/> to serialise each of the <see cref="Array"/>s
        /// elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="Array"/> to serialise to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> holding the <see cref="Array"/>.</param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/> to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="Array"/> held in the given <see cref="PropertyInfo"/> is null, or if the <see cref="Array"/>s
        /// elements do not have a type.
        /// </exception>
        private void SerialiseArrayToWriter(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type elementType = propertyInfo.PropertyType.GetElementType();
            Array array = (Array)propertyInfo.GetValue(obj);

            binaryWriter.Write(array?.Length ?? 0);

            if (elementType.IsClass && !PacketConverterHelper.TypeIsPrimitive(elementType))
            {
                foreach (object element in array)
                {
                    SerialiseObjectToWriter(element, binaryWriter);
                }
            }
            else //Primitive element type
            {
                foreach (object primitiveElement in array)
                {
                    dynamic primitiveValue = primitiveElement;
                    binaryWriter.Write(primitiveValue);
                }
            }
        }

        /// <summary>
        /// Serialises the given <see cref="IList"/> to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// Uses <see cref="SerialiseObjectToWriter(object,BinaryWriter)"/> to serialise each of the <see cref="IList"/>s
        /// elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="IList"/> to serialise to the given <see cref="BinaryWriter"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> holding the <see cref="IList"/>. </param>
        /// <param name="binaryWriter">
        /// The <see cref="BinaryWriter"/> to whose underlying <see cref="MemoryStream"/> to serialise the given <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="IList"/> held in the given <see cref="PropertyInfo"/> is null, or if the <see cref="IList"/>s
        /// elements do not have a type.
        /// </exception>
        private void SerialiseListToWriter(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type elementType = propertyInfo.PropertyType.GetGenericArguments()[0];

            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            IEnumerable currentList = (IEnumerable)propertyInfo.GetValue(obj);

            foreach (object element in currentList)
            {
                list.Add(element);
            }

            binaryWriter.Write(list.Count);

            if (elementType.IsClass && !PacketConverterHelper.TypeIsPrimitive(elementType))
            {
                foreach (object element in list)
                {
                    SerialiseObjectToWriter(element, binaryWriter);
                }
            }
            else //Primitive type
            {
                foreach (object primitiveElement in list)
                {
                    dynamic primitiveValue = primitiveElement;
                    binaryWriter.Write(primitiveValue);
                }
            }
        }

        #endregion Serialisation

        #region Deserialisation

        /// <summary>
        /// Deserialises all the properties on the given <see cref="object"/> that can be deserialised from the given
        /// <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> whose properties to deserialise using the given <see cref="BinaryReader"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/> to deserialise the properties
        /// of the given <see cref="object"/>.
        /// </param>
        /// <returns>The given <see cref="object"/> with all deserialisable properties set.</returns>
        /// <remarks>
        /// This method can only deserialise properties that lack the custom <see cref="PacketIgnorePropertyAttribute"/>.
        /// Any other properties will be left at their default values.
        /// </remarks>
        private object DeserialiseObjectFromReader(object obj, BinaryReader binaryReader)
        {
            PropertyInfo[] propertiesToDeserialise = GetTypeProperties(obj.GetType());

            for (int i = 0; i < propertiesToDeserialise.Length; i++)
            {
                propertiesToDeserialise[i].SetValue(obj, DeserialiseObjectFromReader(obj, propertiesToDeserialise[i], binaryReader));
            }

            return obj;
        }

        /// <summary>
        /// Deserialises the given <see cref="PropertyInfo"/> from the given <see cref="BinaryReader"/>s underlying
        /// <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="obj"> The <see cref="object"/> whose <see cref="PropertyInfo"/> value to deserialise.</param>
        /// <param name="propertyInfo">
        /// The <see cref="PropertyInfo"/> to deserialise from the given <see cref="BinaryReader"/>s underlying
        /// <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/> to deserialise the given
        /// <see cref="PropertyInfo"/>.
        /// </param>
        /// <returns>
        /// The <see cref="object"/> deserialised from the <see cref="MemoryStream"/>. This can be null if the
        /// <see cref="ObjectState"/> is <see cref="ObjectState.Null"/>.
        /// </returns>
        private object DeserialiseObjectFromReader(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            Type propertyType = propertyInfo.PropertyType;

            //We have an enumeration
            if (propertyType.IsEnum)
            {
                return Enum.Parse(propertyType, binaryReader.ReadString());
            }

            //We have an array
            if (propertyType.IsArray)
            {
                return ReadArrayFromStream(obj, propertyInfo, binaryReader);
            }

            //We have a generic list
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ReadListFromStream(obj, propertyInfo, binaryReader);
            }

            ObjectState objectState = (ObjectState)binaryReader.ReadByte();

            if (PacketConverterHelper.TypeIsPrimitive(propertyType)) //We have a primitive type
            {
                //If the primitive object is null we just return null, otherwise we deserialise from the stream
                return objectState == ObjectState.NotNull
                    ? ReadPrimitiveFromStream(propertyType, binaryReader)
                    : null;
            }

            //We have a complex type
            //If the custom object is null we just return null, otherwise we deserialise from the stream
            return objectState == ObjectState.NotNull
                ? DeserialiseObjectFromReader(PacketConverterHelper.InstantiateObject(propertyType), binaryReader)
                : null;
        }

        /// <summary>
        /// Deserialises the given <see cref="Array"/> from the given <see cref="BinaryReader"/>s underlying
        /// <see cref="MemoryStream"/>. Uses <see cref="DeserialiseObjectFromReader(object,BinaryReader)"/> to serialise
        /// each of the <see cref="Array"/>s elements to the stream.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="Array"/> to deserialise from the given <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> holding the <see cref="Array"/>.</param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/> to deserialise the given
        /// <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="Array"/>s elements do not have a type.</exception>
        private Array ReadArrayFromStream(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int arraySize = binaryReader.ReadInt32();

            Type elementType = propertyInfo.PropertyType.GetElementType();
            Array array = Array.CreateInstance(elementType, arraySize);

            for (int i = 0; i < arraySize; ++i)
            {
                if (elementType.IsClass && !PacketConverterHelper.TypeIsPrimitive(elementType))
                {
                    array.SetValue(DeserialiseObjectFromReader(PacketConverterHelper.InstantiateObject(elementType), binaryReader), i);
                }
                else
                {
                    array.SetValue(ReadPrimitiveFromStream(elementType, binaryReader), i);
                }
            }

            return array;
        }

        /// <summary>
        /// Deserialises the given <see cref="IList"/> from the given <see cref="BinaryReader"/>s underlying
        /// <see cref="MemoryStream"/>. Uses <see cref="DeserialiseObjectFromReader(object,BinaryReader)"/> to serialise
        /// each of the <see cref="IList"/>s elements to the stream.
        /// </summary>
        /// <param name="obj"> The <see cref="IList"/> to deserialise from the given <see cref="BinaryReader"/>s underlying
        /// <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> holding the <see cref="IList"/>.</param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/> to deserialise the given
        /// <see cref="PropertyInfo"/>.
        /// </param>
        /// <exception cref="NullReferenceException">
        /// Thrown if the <see cref="IList"/> held in the <see cref="MemoryStream"/> is null, or if the <see cref="IList"/>s
        /// elements do not have a type.
        /// </exception>
        private IList ReadListFromStream(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int listSize = binaryReader.ReadInt32();

            Type listType = propertyInfo.PropertyType.GetGenericArguments()[0];

            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));

            for (int i = 0; i < listSize; ++i)
            {
                if (listType.IsClass && !PacketConverterHelper.TypeIsPrimitive(listType))
                {
                    list.Add(DeserialiseObjectFromReader(PacketConverterHelper.InstantiateObject(listType), binaryReader));
                }
                else
                {
                    list.Add(ReadPrimitiveFromStream(listType, binaryReader));
                }
            }

            return list;
        }

        /// <summary>
        /// Reads a primitive type from the given <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>
        /// and returns it.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> of the primitive to read from the given <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </param>
        /// <param name="binaryReader">
        /// The <see cref="BinaryReader"/> from whose underlying <see cref="MemoryStream"/> to read the primitive.
        /// </param>
        /// <returns>
        /// The primitive that was read from the given <see cref="BinaryReader"/>s underlying <see cref="MemoryStream"/>.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown whenever a <see cref="Type"/> is passed to this method that is not a primitive.
        /// </exception>
        private object ReadPrimitiveFromStream(Type type, BinaryReader binaryReader)
        {
            //Handling the case where a nullable type gets sent
            Type underlyingNullableType = Nullable.GetUnderlyingType(type);

            if (underlyingNullableType != null)
            {
                type = underlyingNullableType;
            }

            #region Reading Primitives From Stream

            if (type == typeof(bool))
            {
                return binaryReader.ReadBoolean();
            }
            else if (type == typeof(byte))
            {
                return binaryReader.ReadByte();
            }
            else if (type == typeof(ushort))
            {
                return binaryReader.ReadUInt16();
            }
            else if (type == typeof(uint))
            {
                return binaryReader.ReadUInt32();
            }
            else if (type == typeof(ulong))
            {
                return binaryReader.ReadUInt64();
            }
            else if (type == typeof(sbyte))
            {
                return binaryReader.ReadSByte();
            }
            else if (type == typeof(short))
            {
                return binaryReader.ReadInt16();
            }
            else if (type == typeof(int))
            {
                return binaryReader.ReadInt32();
            }
            else if (type == typeof(long))
            {
                return binaryReader.ReadInt64();
            }
            else if (type == typeof(string))
            {
                return binaryReader.ReadString();
            }
            else if (type == typeof(char))
            {
                return binaryReader.ReadChar();
            }
            else if (type == typeof(float))
            {
                return binaryReader.ReadSingle();
            }
            else if (type == typeof(double))
            {
                return binaryReader.ReadDouble();
            }
            else if (type == typeof(decimal))
            {
                return binaryReader.ReadDecimal();
            }

            #endregion Reading Primitives From Stream

            //If we reached here then we were not given a primitive type. This is not supported.
            throw new NotSupportedException();
        }

        #endregion Deserialisation

        #endregion Methods
    }
}