#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-10-2016
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

using System;

namespace Network.Attributes
{
    /// <summary>
    /// In order to generate an answer out of a question, the relations
    /// between the request and response packets have to be set.
    /// E.g NameResponse -> NameRequest.
    /// </summary>
    public class PacketRequestAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PacketRequestAttribute"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public PacketRequestAttribute(Type type)
        {
            RequestType = type;
        }

        /// <summary>
        /// Gets the type of the request.
        /// </summary>
        /// <value>The type of the request.</value>
        public Type RequestType { get; private set; }
    }
}