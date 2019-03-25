#region Licence - LGPLv3

// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 01-31-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 29-09-2018
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

using Network.Interfaces;
using Network.RSA;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Network
{
    public abstract class ConnectionContainer : IRSACapability
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        public ConnectionContainer(string ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;
        }

        /// <summary>
        /// Gets the ip address this container is connected to.
        /// </summary>
        /// <value>The ip address.</value>
        public string IPAddress { get; protected set; }

        /// <summary>
        /// Gets the port this container is connected to.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; protected set; }

        /// <summary>
        /// The PublicKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PublicKey => RSAPair?.Public;

        /// <summary>
        /// The PrivateKey of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public string PrivateKey => RSAPair?.Private;

        /// <summary>
        /// The used KeySize of this instance.
        /// </summary>
        [Obsolete("Use 'RSAPair' instead.")]
        public int KeySize => RSAPair?.KeySize ?? -1;

        /// <summary>
        /// Gets or sets the RSA-Pair.
        /// </summary>
        /// <value>The RSA pair.</value>
        public RSAPair RSAPair { get; set; }

        protected List<Assembly> KnownTypes { get; private set; } = new List<Assembly>();

        /// <summary>
        /// Adds known types to the TCP and UDP connection as soon
        /// as a connection has been established. This is not essential, but will speed up the initial time.
        /// Be aware that this method has te be called from the server and the clientConnectionContainer with the same parameter.
        /// Else the server or the client will crash, because of unknown types.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void AddKownType(Assembly assembly)
        {
            if (KnownTypes.Contains(assembly)) return;
            KnownTypes.Add(assembly);
        }

        /// <summary>
        /// Removes the known type from the init process.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void RemoveKnownType(Assembly assembly)
        {
            if (!KnownTypes.Contains(assembly)) return;
            KnownTypes.Remove(assembly);
        }
    }
}