#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 27.08.2018
//
// Last Modified By : Thomas
// Last Modified On : 27.08.2018
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
using System.Threading.Tasks;
using System;

namespace Network.RSA
{
    public class SecureClientConnectionContainer : ClientConnectionContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="publicKey">The Public-Key.</param>
        /// <param name="privateKey">The Private-Key.</param>
        /// <param name="keySize">The KeySize.</param>
        internal SecureClientConnectionContainer(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048) 
            : base(ipAddress, port)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <param name="udpConnection">The UDP connection.</param>
        /// <param name="publicKey">The Public-Key.</param>
        /// <param name="privateKey">The Private-Key.</param>
        /// <param name="keySize">The KeySize.</param>
        internal SecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection, string publicKey, string privateKey, int keySize = 2048)
            : base(tcpConnection.IPRemoteEndPoint.Address.ToString(), tcpConnection.IPRemoteEndPoint.Port)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Creates a new SecureTcpConnection.
        /// </summary>
        /// <returns>A TcpConnection.</returns>
        protected override async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnection() => await ConnectionFactory.CreateSecureTcpConnectionAsync(IPAddress, Port, PublicKey, PrivateKey, KeySize);

        /// <summary>
        /// Creates a new SecureUdpConnection from the existing SecureTcpConnection.
        /// </summary>
        /// <returns>A UdpConnection.</returns>
        protected override async Task<Tuple<UdpConnection, ConnectionResult>> CreateUdpConnection() => await ConnectionFactory.CreateSecureUdpConnectionAsync(TcpConnection, PublicKey, PrivateKey, KeySize);
    }
}
