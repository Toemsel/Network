#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : NetworkTestClient
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
using Network;
using Network.Enums;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// Simple example>
    /// 1. Establish a connection
    /// 2. Subscribe connectionEstablished event
    /// 3. Register what happens if we receive a packet of type "CalculationResponse"
    /// 4. Send a calculation request.
    /// 5. CalculationResponse received.
    /// </summary>
    public class DelegateExample
    {
        private ClientConnectionContainer container;

        public void Demo()
        {
            //1. Establish a connection to the server.
            container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += connectionEstablished;
        }

        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{type.ToString()} Connection established");
            //3. Register what happens if we receive a packet of type "CalculationResponse"
            container.RegisterPacketHandler<CalculationResponse>(calculationResponseReceived, this);
            //4. Send a calculation request.
            connection.Send(new CalculationRequest(10, 10), this);
        }

        private void calculationResponseReceived(CalculationResponse response, Connection connection)
        {
            //5. CalculationResponse received.
            Console.WriteLine($"Answer received {response.Result}");
        }
    }
}
