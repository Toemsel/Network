using Network;
using System;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// Simple example>
    /// 1. Establish a connection
    /// 2. Subscribe connectionEstablished event
    /// 3. Send and receive a packet
    /// </summary>
    public class AsyncExample
    {
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async void Demo()
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            var tcpConnection = await ConnectionFactory.CreateTcpConnectionAsync("127.0.0.1", 1234);

            //1. Establish a connection to the server.
            ClientConnectionContainer container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += async (connection, type) =>
            {
                connection.EnableLogging = true;
                Console.WriteLine($"{type.ToString()} Connection established");
                //3. Send a request packet async and directly receive an answer.
                CalculationResponse response = await connection.SendAsync<CalculationResponse>(new CalculationRequest(10, 10));
                Console.WriteLine($"Answer received {response.Result}");
            };
        }
    }
}