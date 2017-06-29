#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 28-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 28-11-2015
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
using System;
using System.Text;
using Network.Packets;

namespace Network.Extensions
{
    /// <summary>
    /// Contains useful extensions to convert a rawData packet directly into an expected data type.
    /// </summary>
    public static class RawDataExtension
    {
        public static short ToInt16(this RawData rawData)
        {
            return BitConverter.ToInt16(rawData.Data, 0);
        }

        public static ushort ToUInt16(this RawData rawData)
        {
            return BitConverter.ToUInt16(rawData.Data, 0);
        }

        public static int ToInt32(this RawData rawData)
        {
            return BitConverter.ToInt32(rawData.Data, 0);
        }

        public static UInt32 ToUInt32(this RawData rawData)
        {
            return BitConverter.ToUInt32(rawData.Data, 0);
        }

        public static long ToInt64(this RawData rawData)
        {
            return BitConverter.ToInt64(rawData.Data, 0);
        }

        public static UInt64 ToUInt64(this RawData rawData)
        {
            return BitConverter.ToUInt64(rawData.Data, 0);
        }

        public static string ToUTF32String(this RawData rawData)
        {
            return Encoding.UTF32.GetString(rawData.Data);
        }

        public static string ToUTF16_BigEndian_String(this RawData rawData)
        {
            return Encoding.BigEndianUnicode.GetString(rawData.Data);
        }

        public static string ToUTF16_LittleEndian_String(this RawData rawData)
        {
            return ToUnicodeString(rawData);
        }

        public static string ToUTF8String(this RawData rawData)
        {
            return Encoding.UTF8.GetString(rawData.Data);
        }

        public static string ToUTF7String(this RawData rawData)
        {
            return Encoding.UTF7.GetString(rawData.Data);
        }

        public static string ToASCIIString(this RawData rawData)
        {
            return Encoding.ASCII.GetString(rawData.Data);
        }

        public static string ToUnicodeString(this RawData rawData)
        {
            return Encoding.Unicode.GetString(rawData.Data);
        }

        public static float ToSingle(this RawData rawData)
        {
            return BitConverter.ToSingle(rawData.Data, 0);
        }

        public static double ToDouble(this RawData rawData)
        {
            return BitConverter.ToDouble(rawData.Data, 0);
        }

        public static char ToChar(this RawData rawData)
        {
            return BitConverter.ToChar(rawData.Data, 0);
        }

        public static bool ToBoolean(this RawData rawData)
        {
            return BitConverter.ToBoolean(rawData.Data, 0);
        }
    }
}
