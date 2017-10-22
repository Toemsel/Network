#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 07-26-2015
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
using System;
using Network.Attributes;
using Network.Enums;

namespace Network
{
    /// <summary>
    /// A packet can be sent over a chosen network connection.
    /// Always make sure that your class, which inherits form this one, implements a default constructor.
    /// Because a default constructor is needed for a dynamic class instantiation (reflection).
    /// Every property in your class will be written to the network stream.
    /// To ignore a property, take a look at <see cref="PacketIgnorePropertyAttribute"/>
    /// Following data types are allowed:
    /// http://www.indie-dev.at/?page_id=461
    /// 
    /// Every other data type will lead to an exception during the serialization process.
    /// </summary>
    public abstract class Packet
    {
        /// <summary>
        /// Gets or sets the ID of the packet.
        /// This is essential to map objects directly to the response from the server.
        /// Do not change! It may lead to internal exceptions.
        /// </summary>
        /// <value>The identifier.</value>
        public int ID { get; set; }

        /// <summary>
        /// Gets the state of the packet.
        /// </summary>
        /// <value>The state of the packet.</value>
        [PacketIgnoreProperty]
        public PacketState State { get; internal set; } = PacketState.Success;

        /// <summary>
        /// Gets the byte size of this packet.
        /// </summary>
        /// <value>The size.</value>
        [PacketIgnoreProperty]
        public int Size { get; internal set; }

        /// <summary>
        /// Gets the time [ms] how long it took to receive this packet.
        /// </summary>
        /// <value>The receive time.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        [PacketIgnoreProperty]
        public int ReceiveTime { get { throw new NotImplementedException(); } internal set { throw new NotImplementedException(); } }

        /// <summary>
        /// Before we convert this packet into a byte array, this method will be called automatically.
        /// </summary>
        public virtual void BeforeSend() { }

        /// <summary>
        /// Before we send this packet to the responsible delegate, this method will be called automatically.
        /// </summary>
        public virtual void BeforeReceive() { }
    }
}