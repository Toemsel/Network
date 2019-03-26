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
// Thomas Christof (c) 2018
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
using System.Reflection;

namespace Network.Packets
{
    /// <summary>
    /// Instructs the paired <see cref="Connection"/> to add all the
    /// <see cref="Type"/>s in the given <see cref="Assembly"/>.
    /// </summary>
    [PacketType(6)]
    internal class AddPacketTypeRequest : RequestPacket
    {
        #region Properties

        /// <summary>
        /// The name of the <see cref="Assembly"/> that should be added.
        /// </summary>
        public string AssemblyName { get; set; }

        #endregion Properties

        #region Constructors

        public AddPacketTypeRequest()
        {
        }

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="AddPacketTypeRequest"/>
        /// class, with the given <see cref="Assembly"/>s name specified.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        public AddPacketTypeRequest(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        #endregion Constructors
    }
}