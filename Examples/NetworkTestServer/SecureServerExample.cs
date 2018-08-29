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
using Network;
using Network.Enums;
using Network.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using TestServerClientPackets;
using TestServerClientPackets.ExamplePacketsOne;

namespace NetworkTestServer
{
    /// <summary>
    /// Simple example>
    /// 1. Retrieve public key.
    /// 2. Retrieve private key.
    /// 3. Start to listen on a port
    /// 4. Applying optional settings
    /// 5. Register packet listeners.
    /// 6. Handle incoming packets.
    /// </summary>
    public class SecureServerExample
    {
        private ServerConnectionContainer secureServerConnectionContainer;

        internal void Demo()
        {
            //1. Retrieve public key
            string publicKey = File.ReadAllText("PublicKey.xml");
            //2. Retrieve private key
            string privateKey = File.ReadAllText("PrivateKey.xml");

            //3. Start to listen on a port
            secureServerConnectionContainer = ConnectionFactory.CreateSecureServerConnectionContainer(1234, publicKey, privateKey, start: false);

            //4. Apply optional settings.
            #region Optional settings
            secureServerConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{secureServerConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            secureServerConnectionContainer.ConnectionEstablished += connectionEstablished;
#if NET46
            secureServerConnectionContainer.AllowBluetoothConnections = true;
#endif
            secureServerConnectionContainer.AllowUDPConnections = true;
            secureServerConnectionContainer.UDPConnectionLimit = 2;            
            #endregion Optional settings

            //Call start here, because we had to enable the bluetooth property at first.
            secureServerConnectionContainer.Start();

            //Don't close the application.
            Console.ReadLine();
        }

        /// <summary>
        /// We got a connection.
        /// </summary>
        /// <param name="connection">The connection we got. (TCP or UDP)</param>
        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            connection.EnableLogging = true;
            Console.WriteLine($"{secureServerConnectionContainer.Count} {connection.GetType()} connected on port {connection.IPRemoteEndPoint.Port}");

            //3. Register packet listeners.
            connection.RegisterStaticPacketHandler<CalculationRequest>(calculationReceived);
            connection.RegisterStaticPacketHandler<AddStudentToDatabaseRequest>(addStudentReceived);
            connection.RegisterRawDataHandler("HelloWorld", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToUTF8String()}"));
            connection.RegisterRawDataHandler("BoolValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToBoolean()}"));
            connection.RegisterRawDataHandler("DoubleValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToDouble()}"));
        }

        /// <summary>
        /// If the client sends us a calculation request, it will end up here.
        /// </summary>
        /// <param name="packet">The calculation packet.</param>
        /// <param name="connection">The connection who was responsible for the transmission.</param>
        private static void calculationReceived(CalculationRequest packet, Connection connection)
        {
            //4. Handle incoming packets.
            connection.Send(new CalculationResponse(packet.X + packet.Y, packet));
        }

        /// <summary>
        /// If the client sends us a add student request, it will end up here.
        /// </summary>
        /// <param name="packet">The add student request packet.</param>
        /// <param name="connection">The connection who was responsible for the transmission.</param>
        private static void addStudentReceived(AddStudentToDatabaseRequest packet, Connection connection)
        {
            //4. Handle incomming packets
            connection.Send(new AddStudentToDatabaseResponse(0, packet));
        }
    }
}