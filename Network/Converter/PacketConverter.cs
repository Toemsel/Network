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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using Network.Extensions;
using System.Runtime.Serialization;

namespace Network.Converter
{
    /// <summary>
    /// Provides extension methods for packets to handle their read and write behaviors.
    /// </summary>
    internal class PacketConverter : IPacketConverter
    {
        /// <summary>
        /// Remember a types propertyInfo to save cpu time.
        /// </summary>
        private Dictionary<Type, PropertyInfo[]> packetProperties = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Converts a given packet to a byte array.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>System.Byte[].</returns>
        public byte[] GetBytes(Packet packet)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            GetBytesFromCustomObject(packet, binaryWriter);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Applies the byte array to the packets properties.
        /// </summary>
        /// <param name="packetType">The type of the final packet.</param>
        /// <param name="data">The data which should be applied.</param>
        public Packet GetPacket(Type packetType, byte[] data)
        {
            Packet packet = CreateInstanceOfPacketType(packetType);
            MemoryStream memoryStream = new MemoryStream(data, 0, data.Length);
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            ReadObjectFromStream(packet, binaryReader);
            return packet;
        }

        /// <summary>
        /// Creates an object instance of a packet-Type.
        /// If there is no default constructor, the object instance will
        /// be created without calling the default constructor.
        /// </summary>
        /// <param name="packetType">Type of the packet.</param>
        /// <returns>Packet.</returns>
        private Packet CreateInstanceOfPacketType(Type packetType)
        {
            if (packetType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null) != null)
                return (Packet)Activator.CreateInstance(packetType);
            else return (Packet)FormatterServices.GetUninitializedObject(packetType);
        }

        /// <summary>
        /// Writes the length of the given array onto the given stream.
        /// Reads the array out of the property and calls the <see cref="GetBytesFromCustomObject"/> to write the object data.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryWriter">The binary writer.</param>
        /// <returns>System.Byte[].</returns>
        private void GetBytesFromArray(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type arrayType = propertyInfo.PropertyType.GetElementType();
            Array propertyData = (Array)propertyInfo.GetValue(obj);
            binaryWriter.Write(propertyData?.Length ?? 0);

            if (arrayType.IsClass && !IsPrimitive(arrayType)) propertyData.GetEnumerator().ToList<object>().ForEach(p => GetBytesFromCustomObject(p, binaryWriter));
            else propertyData.GetEnumerator().ToList<object>().ForEach(p => { dynamic targetType = p; binaryWriter.Write(targetType); });
        }

        /// <summary>
        /// Writes the length of the given list onto the given stream.
        /// Reads the list out of the property and calls the <see cref="GetBytesFromCustomObject"/> to write the object data.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryWriter">The binary writer.</param>
        /// <returns>System.Byte[].</returns>
        private void GetBytesFromList(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            Type listType = propertyInfo.PropertyType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
            ((IEnumerable)propertyInfo.GetValue(obj))?.GetEnumerator().ToList<object>().ForEach(o => list.Add(o));
            binaryWriter.Write(list.Count);

            if (listType.IsClass && !IsPrimitive(listType)) list.GetEnumerator().ToList<object>().ForEach(o => GetBytesFromCustomObject(o, binaryWriter));
            else list.GetEnumerator().ToList<object>().ForEach(o => { dynamic targetType = o; binaryWriter.Write(targetType); });
        }

        /// <summary>
        /// Gets the bytes from custom object. It searches for all propertyInfos and calls the next method.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="binaryWriter">The binary writer.</param>
        private void GetBytesFromCustomObject(object obj, BinaryWriter binaryWriter)
        {
            GetPacketProperties(obj).ToList().ForEach(p => GetBytesFromCustomObject(obj, p, binaryWriter));
        }

        /// <summary>
        /// Writes the data of all the properties in place on the given binary stream.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryWriter">The binary writer.</param>
        /// <returns>System.Byte[].</returns>
        private void GetBytesFromCustomObject(object obj, PropertyInfo propertyInfo, BinaryWriter binaryWriter)
        {
            dynamic propertyValue = propertyInfo.GetValue(obj);
            if (propertyInfo.PropertyType.IsEnum) //Enums are an exception.
                binaryWriter.Write(propertyValue.ToString());
            else if (propertyInfo.PropertyType.IsArray)
                GetBytesFromArray(obj, propertyInfo, binaryWriter);
            else if (propertyInfo.PropertyType.IsGenericType &&
                propertyInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(List<>)))
                GetBytesFromList(obj, propertyInfo, binaryWriter);
            else if (!IsPrimitive(propertyInfo) && propertyValue != null) //Primitive or object
            {
                binaryWriter.Write((byte)ObjectState.NOT_NULL); //Mark it as a NOT NULL object.
                GetBytesFromCustomObject(propertyValue, binaryWriter);
            }
            else if (!IsPrimitive(propertyInfo) && propertyValue == null)
                binaryWriter.Write((byte)ObjectState.NULL); //Mark it as a NULL object.
            else
            {
                byte objectStatus = propertyValue == null ? (byte)ObjectState.NULL : (byte)ObjectState.NOT_NULL;
                binaryWriter.Write(objectStatus);

                if (propertyValue != null)
                    binaryWriter.Write(propertyValue);
            }
        }

        /// <summary>
        /// Reads the length of the array from the stream and creates the array instance.
        /// Also fills the array by using recursion.  
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Array.</returns>
        private Array ReadArrayFromStream(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int arraySize = binaryReader.ReadInt32();
            Type arrayType = propertyInfo.PropertyType.GetElementType();
            Array propertyData = Array.CreateInstance(arrayType, arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                if (arrayType.IsClass && !IsPrimitive(arrayType)) propertyData.SetValue(ReadObjectFromStream(Activator.CreateInstance(arrayType), binaryReader), i);
                else propertyData.SetValue(ReadPrimitiveFromStream(arrayType, binaryReader), i);
            }

            return propertyData;
        }

        /// <summary>
        /// Reads the length of the list from the stream and creates the list instance.
        /// Also fills the list by using recursion.  
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Collections.IList.</returns>
        private IList ReadListFromStream(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int listSize = binaryReader.ReadInt32();
            Type listType = propertyInfo.PropertyType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
            for (int i = 0; i < listSize; i++)
            {
                if (listType.IsClass && !IsPrimitive(listType)) list.Add(ReadObjectFromStream(Activator.CreateInstance(listType), binaryReader));
                else list.Add(ReadPrimitiveFromStream(listType, binaryReader));
            }

            return list;
        }

        /// <summary>
        /// Reads all the properties from an object and calls the <see cref="ReadObjectFromStream"/> method.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Object.</returns>
        private object ReadObjectFromStream(object obj, BinaryReader binaryReader)
        {
            GetPacketProperties(obj).ToList().ForEach(p => p.SetValue(obj, ReadObjectFromStream(obj, p, binaryReader)));
            return obj; //All properties set with in place.
        }

        /// <summary>
        /// Reads the object from stream. Can differ between:
        /// - Primitives
        /// - Lists
        /// - Arrays
        /// - Classes (None list + arrays)
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Object.</returns>
        private object ReadObjectFromStream(object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            if (propertyInfo.PropertyType.IsArray)
                return ReadArrayFromStream(obj, propertyInfo, binaryReader);
            else if (propertyInfo.PropertyType.IsGenericType &&
                propertyInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(List<>)))
                return ReadListFromStream(obj, propertyInfo, binaryReader);
            else if (propertyInfo.PropertyType.IsEnum)
                return Enum.Parse(propertyInfo.PropertyType, binaryReader.ReadString());
            else if (!IsPrimitive(propertyInfo))
            {
                ObjectState objectState = (ObjectState)binaryReader.ReadByte();
                if (objectState == ObjectState.NOT_NULL)
                    return ReadObjectFromStream(Activator.CreateInstance(propertyInfo.PropertyType), binaryReader);
                return null; //The object we received is null. So return nothing.
            }
            else return ReadPrimitiveFromStream(propertyInfo, binaryReader);
        }

        /// <summary>
        /// Reads a primitive from a stream.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Object.</returns>
        private object ReadPrimitiveFromStream(PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            ObjectState objectState = (ObjectState)binaryReader.ReadByte();

            if(objectState == ObjectState.NOT_NULL)
                return ReadPrimitiveFromStream(propertyInfo.PropertyType, binaryReader);

            return null;
        }

        /// <summary>
        /// Reads a primitive from a stream.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Object.</returns>
        private object ReadPrimitiveFromStream(Type type, BinaryReader binaryReader)
        {
            Type underlyingNullableType = Nullable.GetUnderlyingType(type);

            // we do have a nullable as an underlying type.
            // Hence, we have to readjust our type value.
            if(underlyingNullableType != null)
                type = underlyingNullableType;

            if (type.Equals(typeof(String)))
                return binaryReader.ReadString();
            else if (type.Equals(typeof(Int16)))
                return binaryReader.ReadInt16();
            else if (type.Equals(typeof(Int32)))
                return binaryReader.ReadInt32();
            else if (type.Equals(typeof(Int64)))
                return binaryReader.ReadInt64();
            else if (type.Equals(typeof(Boolean)))
                return binaryReader.ReadBoolean();
            else if (type.Equals(typeof(Byte)))
                return binaryReader.ReadByte();
            else if (type.Equals(typeof(Char)))
                return binaryReader.ReadChar();
            else if (type.Equals(typeof(Decimal)))
                return binaryReader.ReadDecimal();
            else if (type.Equals(typeof(Double)))
                return binaryReader.ReadDouble();
            else if (type.Equals(typeof(Single)))
                return binaryReader.ReadSingle();
            else if (type.Equals(typeof(UInt16)))
                return binaryReader.ReadUInt16();
            else if (type.Equals(typeof(UInt32)))
                return binaryReader.ReadUInt32();
            else if (type.Equals(typeof(UInt64)))
                return binaryReader.ReadUInt64();

            //Only primitive types are supported in this method.
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the specified property information is primitive.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>System.Boolean.</returns>
        private bool IsPrimitive(PropertyInfo propertyInfo) => IsPrimitive(propertyInfo.PropertyType);

        /// <summary>
        /// Determines whether the specified type is primitive.
        /// </summary>
        /// <param name="type">The type to check for primitive</param>
        /// <returns>Is Primitive or not.</returns>
        private bool IsPrimitive(Type type) => type.Namespace.Equals("System");

        /// <summary>
        /// Extracts all properties from a packet which do not have a packetIgnoreProperty attribute.
        /// </summary>
        /// <param name="packet">The packet to extract the property infos</param>
        /// <returns>The property infos.</returns>
        private PropertyInfo[] GetPacketProperties(object packet)
        {
            lock (packetProperties)
            {
                if (packetProperties.ContainsKey(packet.GetType()))
                    return packetProperties[packet.GetType()];
                PropertyInfo[] propInfos = packet.GetType().GetProperties().ToList().Where(p => p.GetCustomAttribute(typeof(PacketIgnorePropertyAttribute)) == null).ToArray();
                packetProperties.Add(packet.GetType(), propInfos);
                return GetPacketProperties(packet);
            }
        }
    }
}
