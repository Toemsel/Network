using System;
using Network;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestServerClientPackets;
using Network.Enums;

namespace NetworkTestServer
{
    class Program
    {
        private ServerConnectionContainer serverConnectionContainer;

        public Program()
        {
            Console.WriteLine("Server started");
        }

        public void Demo()
        {
            ConnectionFactory.AddKnownTypes(typeof(CalculationRequest)); //ToDo: Remove after the update.
            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(1234);

            /* Optional settings */
            serverConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            serverConnectionContainer.ConnectionEstablished += connectionEstablished;
            /* END optional settings */
        }

        /// <summary>
        /// We got a connection.
        /// </summary>
        /// <param name="connection">The connection we got. (TCP or UDP)</param>
        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{connection.GetType()} connected on port {connection.IPLocalEndPoint.Port}");
            connection.RegisterPacketHandler(typeof(CalculationRequest), calculationReceived);
        }

        /// <summary>
        /// If the client sends us a calculation request, it will end up here.
        /// </summary>
        /// <param name="packet">The calculation packet.</param>
        /// <param name="connection">The connection who was responsible for the transmission.</param>
        private void calculationReceived(Packet packet, Connection connection)
        {
            CalculationRequest request = (CalculationRequest)packet;
            connection.Send(new CalculationResponse(request.X + request.Y));
        }

        static void Main(string[] args)
        {
            new Program().Demo();
            Console.ReadLine();
        }
    }
}
