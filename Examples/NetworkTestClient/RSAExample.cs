#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : NetworkTestClient
// Author           : Thomas Christof
// Created          : 27-08-2018
//
// Last Modified By : Thomas Christof
// Last Modified On : 27-08-2018
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
using System.IO;
using Network;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// RSA example>
    /// 1. Retrieve public key
    /// 2. Retrieve private key
    /// 3. Establish a connection
    /// 4. Register Connection-Received Handlers
    /// 5. Register Packet-Handlers.
    /// 6. Send and receive a packet
    /// </summary>
    public class RSAExample
    {
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        public async void Demo()
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            //1. Retrieve public key
            string publicKey = File.ReadAllText("PublicKey.xml");
            //2. Retrieve private key
            string privateKey = File.ReadAllText("PrivateKey.xml");
            //3. Establish a connection.
            ClientConnectionContainer container = ConnectionFactory.CreateSecureClientConnectionContainer("127.0.0.1", 1234, publicKey, privateKey);
            //4. Register what happens if we get a connection
            container.ConnectionEstablished += (connection, type) =>
            {
                Console.WriteLine($"{type.ToString()} Connection established");
                //5. Register what happens if we receive a packet of type "CalculationResponse"
                connection.RegisterPacketHandler<CalculationResponse>((response, con) => Console.WriteLine($"Answer received {response.Result}"), this);
                //6. Send a calculation request.
                connection.Send(new CalculationRequest(10, 10), this);
            };
        }
    }
}