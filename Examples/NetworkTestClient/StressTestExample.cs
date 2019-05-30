using Network;
using Network.Enums;
using System;
using System.Threading;
using TestServerClientPackets;

namespace NetworkTestClient
{
    public class StressTestExample
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

            CreateAndRunWorkerThread();
        }

        private void calculationResponseReceived(CalculationResponse response, Connection connection)
        {
            //5. CalculationResponse received.
            Console.WriteLine($"Answer received {response.Result}");
        }

        private void CreateAndRunWorkerThread()
        {
            Thread thread = new Thread(Work);
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        private void Work()
        {
            Random random = new Random();

            for (int index = 0; index < random.Next(ushort.MaxValue, ushort.MaxValue * 256); index++)
            {
                container.Send(new CalculationRequest(random.Next(0, int.MaxValue), random.Next(0, int.MaxValue)), this);
                Thread.Sleep(1);
            }
        }
    }
}
