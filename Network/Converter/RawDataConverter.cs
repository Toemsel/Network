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

using Network.Packets;

using System;
using System.Text;

namespace Network.Converter
{
    /// <summary>
    /// Converts raw primitive type values into a <see cref="RawData"/> packet
    /// that can be sent across the network.
    /// </summary>
    public static class RawDataConverter
    {
        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="bool"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromBoolean(string key, bool value)
        {
            return new RawData(key, GetBytes(value));
        }

        #region Unsigned Integer Conversion

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="byte"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUInt8(string key, byte value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="ushort"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUInt16(string key, ushort value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="uint"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUInt32(string key, uint value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="ulong"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUInt64(string key, ulong value)
        {
            return new RawData(key, GetBytes(value));
        }

        #endregion Unsigned Integer Conversion

        #region Signed Integer Conversion

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="sbyte"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromInt16(string key, sbyte value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="short"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromInt16(string key, short value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="int"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromInt32(string key, int value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="long"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromInt64(string key, long value)
        {
            return new RawData(key, GetBytes(value));
        }

        #endregion Signed Integer Conversion

        #region String Conversion

        #region Unicode Encoding

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.BigEndianUnicode"/> encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUTF16_BigEndian_String(string key, string value)
        {
            return new RawData(key, Encoding.BigEndianUnicode.GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.Unicode"/> (little endian) encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUTF16_LittleEndian_String(string key, string value)
        {
            return new RawData(key, Encoding.Unicode.GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.Unicode"/> encoding. Identical to
        /// <see cref="FromUTF16_LittleEndian_String"/> method.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUnicodeString(string key, string value)
        {
            return new RawData(key, Encoding.Unicode.GetBytes(value));
        }

        #endregion Unicode Encoding

        #region UTFXXX Encoding

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.UTF32"/> encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUTF32String(string key, string value)
        {
            return new RawData(key, Encoding.UTF32.GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.UTF8"/> encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUTF8String(string key, string value)
        {
            return new RawData(key, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.UTF7"/> encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromUTF7String(string key, string value)
        {
            return new RawData(key, Encoding.UTF7.GetBytes(value));
        }

        #endregion UTFXXX Encoding

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="string"/>
        /// value, using the <see cref="Encoding.ASCII"/> encoding.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromASCIIString(string key, string value)
        {
            return new RawData(key, Encoding.ASCII.GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="char"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromChar(string key, char value)
        {
            return new RawData(key, GetBytes(value));
        }

        #endregion String Conversion

        #region Floating Point Conversion

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="float"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromSingle(string key, float value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="double"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromDouble(string key, double value)
        {
            return new RawData(key, GetBytes(value));
        }

        /// <summary>
        /// Returns a <see cref="RawData"/> holding the given <see cref="decimal"/>
        /// value.
        /// </summary>
        /// <param name="key">
        /// The key to use for the <see cref="RawData"/> packet.
        /// </param>
        /// <param name="value">
        /// The primitive value to send.
        /// </param>
        /// <returns>
        /// A <see cref="RawData"/> packet holding the given primitive, with the
        /// given key.
        /// </returns>
        public static RawData FromDecimal(string key, decimal value)
        {
            return new RawData(key, GetBytes(value));
        }

        #endregion Floating Point Conversion

        /// <summary>
        /// Converts the given value into bytes and returns them.
        /// </summary>
        /// <param name="value">
        /// The value to convert into bytes.
        /// </param>
        /// <returns>
        /// The byte array of the serialised value.
        /// </returns>
        public static byte[] GetBytes(dynamic value)
        {
            return BitConverter.GetBytes(value);
        }
    }
}