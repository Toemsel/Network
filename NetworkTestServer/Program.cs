#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : NetworkTestServer
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
using System;
using System.Text;
using Network;
using Network.Enums;
using Network.Extensions;
using TestServerClientPackets;
using TestServerClientPackets.ExamplePacketsOne;
using TestServerClientPackets.ExamplePacketsOne.Containers;

namespace NetworkTestServer
{
    /// <summary>
    /// Simple example>
    /// 1. Start to listen on a port
    /// 2. Applying optional settings
    /// 3. Register packet listeners.
    /// 4. Handle incoming packets.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            new Program().Demo();
            Console.ReadLine();
        }

        private ServerConnectionContainer serverConnectionContainer;

        public void Demo()
        {
            //1. Start listen on a port
            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(1234, false);

            //2. Apply optional settings.
            #region Optional settings
            serverConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{serverConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            serverConnectionContainer.ConnectionEstablished += connectionEstablished;
            serverConnectionContainer.AllowBluetoothConnections = true;
            serverConnectionContainer.AllowUDPConnections = true;
            serverConnectionContainer.UDPConnectionLimit = 2;
            #endregion Optional settings

            //Call start here, because we had to enable the bluetooth property at first.
            serverConnectionContainer.Start();
        }

        /// <summary>
        /// We got a connection.
        /// </summary>
        /// <param name="connection">The connection we got. (TCP or UDP)</param>
        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{serverConnectionContainer.Count} {connection.GetType()} connected on port {connection.IPRemoteEndPoint.Port}");

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
            connection.Send(new AddStudentToDatabaseResponse((DatabaseResult)0, packet));
        }
    }
}
