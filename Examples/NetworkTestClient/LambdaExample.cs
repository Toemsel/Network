using Network;
using System;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// Simple example>
    /// 1. Establish a connection
    /// 2. Subscribe connectionEstablished event
    /// 3. Register what happens if we receive a packet of type "CalculationResponse"
    /// 4. Send a calculation request.
    /// </summary>
    public class LambdaExample
    {
        public void Demo()
        {
            //1. Establish a connection to the server.
            ClientConnectionContainer container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += (connection, type) =>
            {
                Console.WriteLine($"{type.ToString()} Connection established");
                //3. Register what happens if we receive a packet of type "CalculationResponse"
                connection.RegisterPacketHandler<CalculationResponse>((response, con) => Console.WriteLine($"Answer received {response.Result}"), this);
                //4. Send a calculation request.
                connection.Send(new CalculationRequest(10, 10), this);
            };
        }
    }
}