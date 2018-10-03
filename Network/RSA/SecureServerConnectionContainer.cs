#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 27.08.2018
//
// Last Modified By : Thomas
// Last Modified On : 29.09.2018
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
using System.Net.Sockets;

namespace Network.RSA
{
    /// <summary>
    /// Is able to open and close connections to clients in a secure way.
    /// Handles basic client connection requests and provides useful methods
    /// to manage the existing connection.
    /// </summary>
    public class SecureServerConnectionContainer : ServerConnectionContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to tcp/udp/bluetooth clients.</param>
        internal SecureServerConnectionContainer(string ipAddress, int port, RSAPair rsaPair, bool start = true)
            : base(ipAddress, port, start)
        {
            RSAPair = rsaPair;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecureServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="rsaPair">RSA-Pair.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        internal SecureServerConnectionContainer(int port, RSAPair rsaPair, bool start = true)
            : this(System.Net.IPAddress.Any.ToString(), port, rsaPair, start) { }

        /// <summary>
        /// Instead of a normal TcpConnection, a secure server connection demands a secureTcpConnection.
        /// </summary>
        /// <param name="tcpClient">The tcpClient to be wrapped.</param>
        /// <returns>A <see cref="SecureTcpConnection"/></returns>
        protected override TcpConnection CreateTcpConnection(TcpClient tcpClient) => ConnectionFactory.CreateSecureTcpConnection(tcpClient, RSAPair);
    }
}