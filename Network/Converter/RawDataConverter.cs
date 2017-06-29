#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 28-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 28-11-2016
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

namespace Network.Converter
{
    public static class RawDataConverter
    {
        public static RawData FromUInt16(string key, UInt16 value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromInt16(string key, short value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromInt32(string key, int value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromUInt32(string key, UInt32 value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromInt64(string key, long value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromUInt64(string key, UInt64 value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromUTF32String(string key, string value)
        {
            return new RawData(key, Encoding.UTF32.GetBytes(value));
        }

        public static RawData FromUTF16_BigEndian_String(string key, string value)
        {
            return new RawData(key, Encoding.BigEndianUnicode.GetBytes(value));
        }

        public static RawData FromUTF16_LittleEndian_String(string key, string value)
        {
            return new RawData(key, Encoding.Unicode.GetBytes(value));
        }

        public static RawData FromUTF8String(string key, string value)
        {
            return new RawData(key, Encoding.UTF8.GetBytes(value));
        }

        public static RawData FromUTF7String(string key, string value)
        {
            return new RawData(key, Encoding.UTF7.GetBytes(value));
        }

        public static RawData FromASCIIString(string key, string value)
        {
            return new RawData(key, Encoding.ASCII.GetBytes(value));
        }

        public static RawData FromUnicodeString(string key, string value)
        {
            return new RawData(key, Encoding.Unicode.GetBytes(value));
        }

        public static RawData FromSingle(string key, float value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromDouble(string key, double value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromChar(string key, char value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static RawData FromBoolean(string key, bool value)
        {
            return new RawData(key, GetBytes(value));
        }

        public static byte[] GetBytes(dynamic value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}
