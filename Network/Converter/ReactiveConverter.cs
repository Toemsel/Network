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
using Network.Packets;

namespace Network.Converter
{
    /// <summary>
    /// Provides extension methods for packets to handle their read and write behaviors.
    /// </summary>
    internal class ReactiveConverter
    {
        /// <summary>
        /// Remember a types propertyInfo to save cpu time.
        /// </summary>
        private Dictionary<Type, PropertyInfo[]> packetProperties = new Dictionary<Type, PropertyInfo[]>();
        private Dictionary<string, Type> assemblyTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Applies the byte array to the packets properties.
        /// </summary>
        /// <param name="packetType">The type of the final packet.</param>
        /// <param name="data">The data which should be applied.</param>
        public Packet GetPacket(Reactive.Reactive reactive, Type packetType, byte[] data)
        {
            Packet packet = ((Packet)Activator.CreateInstance(packetType));
            MemoryStream memoryStream = new MemoryStream(data, 0, data.Length);
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            ReadObjectFromStream(reactive, packet, binaryReader);
            return packet;
        }

        /// <summary>
        /// Reads the length of the array from the stream and creates the array instance.
        /// Also fills the array by using recursion.  
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Array.</returns>
        private Array ReadArrayFromStream(Reactive.Reactive reactive, object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int arraySize = binaryReader.ReadInt32();
            Type arrayType = propertyInfo.PropertyType.GetElementType();
            Array propertyData = Array.CreateInstance(arrayType, arraySize);
            for (int i = 0; i < arraySize; i++)
            {
                if (arrayType.IsClass && !IsPrimitive(arrayType)) propertyData.SetValue(ReadObjectFromStream(reactive, Activator.CreateInstance(arrayType), binaryReader), i);
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
        private IList ReadListFromStream(Reactive.Reactive reactive, object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            int listSize = binaryReader.ReadInt32();
            Type listType = propertyInfo.PropertyType.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
            for (int i = 0; i < listSize; i++)
            {
                if (listType.IsClass && !IsPrimitive(listType)) list.Add(ReadObjectFromStream(reactive, Activator.CreateInstance(listType), binaryReader));
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
        private object ReadObjectFromStream(Reactive.Reactive reactive, object obj, BinaryReader binaryReader)
        {
            GetPacketProperties(obj).ToList().ForEach(p => p.SetValue(obj, ReadObjectFromStream(reactive, obj, p, binaryReader)));
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
        private object ReadObjectFromStream(Reactive.Reactive reactive, object obj, PropertyInfo propertyInfo, BinaryReader binaryReader)
        {
            if (propertyInfo.PropertyType.IsArray)
                return ReadArrayFromStream(reactive, obj, propertyInfo, binaryReader);
            else if (propertyInfo.PropertyType.IsGenericType &&
                propertyInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(List<>)))
                return ReadListFromStream(reactive, obj, propertyInfo, binaryReader);
            else if (propertyInfo.PropertyType.IsEnum)
                return ReadPrimitiveFromStream(propertyInfo, binaryReader);
            else if (!IsPrimitive(propertyInfo))
            {
                ObjectState objectState = (ObjectState)binaryReader.ReadByte();
                if (objectState == ObjectState.NOT_NULL)
                {
                    AddReactiveObject packet = (AddReactiveObject)obj;
                    var actualReactiveObjectType = GetTypeFromString(packet.AssemblyName, packet.ReactiveObjectType);
                    if (actualReactiveObjectType != null)
                        return ReadObjectFromStream(reactive, Activator.CreateInstance(actualReactiveObjectType), binaryReader);
                }
                return null; //The object we received is null. So return nothing.
            }
            else if (propertyInfo.PropertyType == typeof(object))
            {
                ReactiveSync reactiveSync = (ReactiveSync)obj;
                var reactiveObjectType = reactive[reactiveSync.ReactiveObjectId];
                return ReadObjectFromStream(reactive, obj, reactiveObjectType.GetProperty(reactiveSync.PropertyName), binaryReader);
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
            return ReadPrimitiveFromStream(propertyInfo.PropertyType, binaryReader);
        }

        /// <summary>
        /// Reads a primitive from a stream.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Object.</returns>
        private object ReadPrimitiveFromStream(Type type, BinaryReader binaryReader)
        {
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
            else if (type.IsEnum)
                return binaryReader.ReadInt32();

            //Only primitive types are supported in this method.
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the specified property information is primitive.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>System.Boolean.</returns>
        private bool IsPrimitive(PropertyInfo propertyInfo)
        {
            return IsPrimitive(propertyInfo.PropertyType);
        }

        /// <summary>
        /// Determines whether the specified type is primitive.
        /// </summary>
        /// <param name="type">The type to check for primitive</param>
        /// <returns>Is Primitive or not.</returns>
        private bool IsPrimitive(Type type)
        {
            return type.Namespace.Equals("System");
        }

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

        /// <summary>
        /// Searches for a type with the given parameters.
        /// </summary>
        /// <param name="assemblyName">The assembly to search within.</param>
        /// <param name="className">The className</param>
        /// <returns></returns>
        private Type GetTypeFromString(string assemblyName, string className)
        {
            lock (assemblyTypes)
            {
                string assemblyClass = assemblyName + className;
                if (assemblyTypes.ContainsKey(assemblyClass))
                    return assemblyTypes[assemblyClass];

                var assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == assemblyName).SingleOrDefault();
                var classType = assembly?.GetTypes().SingleOrDefault(t => t.FullName == className);
                assemblyTypes.Add(assemblyClass, classType);
                return GetTypeFromString(assemblyName, className);
            }
        }
    }
}
