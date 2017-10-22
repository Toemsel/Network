#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-11-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
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
    /// Sending unknown packets needs at first a addPacketTypeRequest to
    /// inform the endPoint about the new incomming packet.
    /// </summary>
    [PacketType(8)]
    internal class AddPacketTypeRequest : RequestPacket
    {
        public AddPacketTypeRequest() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPacketType"/> class.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        public AddPacketTypeRequest(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        /// <summary>
        /// Gets or sets the name of the assembly.
        /// </summary>
        /// <value>The name of the assembly.</value>
        public string AssemblyName { get; set; }
    }
}
