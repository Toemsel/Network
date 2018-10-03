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
// Thomas Christof (c) 2015
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

namespace Network.Packets
{
    /// <summary>
    /// Represends raw data containing anything the programmer wants to send.
    /// </summary>
    [PacketType(15)]
    public class RawData : Packet
    {
        public RawData(string key, byte[] data)
        {
            Key = key;
            Data = data;
        }

        public RawData() { }

        /// <summary>
        /// The key both connections are able to register methods to.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The raw data.
        /// </summary>
        public byte[] Data { get; set; }
    }
}
