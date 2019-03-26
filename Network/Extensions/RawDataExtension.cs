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

using Network.Converter;
using Network.Packets;
using System;
using System.Text;

namespace Network.Extensions
{
    /// <summary>
    /// Provides additional functionality to the <see cref="RawData"/>
    /// class.
    /// </summary>
    public static class RawDataExtension
    {
        #region Methods

        /// <inheritdoc cref="RawDataConverter.ToBoolean"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static bool ToBoolean(this RawData rawData)
        {
            return RawDataConverter.ToBoolean(rawData);
        }

        #region Unsigned Integer Conversion

        /// <inheritdoc cref="RawDataConverter.ToUInt8"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static byte ToUInt8(this RawData rawData)
        {
            return RawDataConverter.ToUInt8(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUInt16"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static ushort ToUInt16(this RawData rawData)
        {
            return RawDataConverter.ToUInt16(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUInt32"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static uint ToUInt32(this RawData rawData)
        {
            return RawDataConverter.ToUInt32(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUInt64"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static ulong ToUInt64(this RawData rawData)
        {
            return RawDataConverter.ToUInt64(rawData);
        }

        #endregion Unsigned Integer Conversion

        #region Signed Integer Conversion

        /// <inheritdoc cref="RawDataConverter.ToInt8"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static sbyte ToInt8(this RawData rawData)
        {
            return RawDataConverter.ToInt8(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToInt16"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static short ToInt16(this RawData rawData)
        {
            return RawDataConverter.ToInt16(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToInt32"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static int ToInt32(this RawData rawData)
        {
            return RawDataConverter.ToInt32(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToInt64"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static long ToInt64(this RawData rawData)
        {
            return RawDataConverter.ToInt64(rawData);
        }

        #endregion Signed Integer Conversion

        #region String Conversion

        #region Unicode Encoding

        /// <inheritdoc cref="RawDataConverter.ToUTF16_BigEndian_String"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUTF16_BigEndian_String(this RawData rawData)
        {
            return RawDataConverter.ToUTF16_BigEndian_String(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUTF16_LittleEndian_String"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUTF16_LittleEndian_String(this RawData rawData)
        {
            return RawDataConverter.ToUTF16_LittleEndian_String(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUnicodeString"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUnicodeString(this RawData rawData)
        {
            return RawDataConverter.ToUnicodeString(rawData);
        }

        #endregion Unicode Encoding

        #region UTFXXX Encoding

        /// <inheritdoc cref="RawDataConverter.ToUTF32String"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUTF32String(this RawData rawData)
        {
            return RawDataConverter.ToUTF32String(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUTF8String"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUTF8String(this RawData rawData)
        {
            return RawDataConverter.ToUTF8String(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToUTF7String"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToUTF7String(this RawData rawData)
        {
            return RawDataConverter.ToUTF7String(rawData);
        }

        #endregion UTFXXX Encoding

        /// <inheritdoc cref="RawDataConverter.ToASCIIString"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static string ToASCIIString(this RawData rawData)
        {
            return RawDataConverter.ToASCIIString(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToChar"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static char ToChar(this RawData rawData)
        {
            return RawDataConverter.ToChar(rawData);
        }

        #endregion String Conversion

        #region Floating Point Conversion

        /// <inheritdoc cref="RawDataConverter.ToSingle"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static float ToSingle(this RawData rawData)
        {
            return RawDataConverter.ToSingle(rawData);
        }

        /// <inheritdoc cref="RawDataConverter.ToDouble"/>
        /// <remarks>
        /// This method should probably not be used, it would be preferable to
        /// use the relevant <see cref="RawDataConverter"/> method instead.
        /// </remarks>
        public static double ToDouble(this RawData rawData)
        {
            return RawDataConverter.ToDouble(rawData);
        }

        #endregion Floating Point Conversion

        #endregion Methods
    }
}