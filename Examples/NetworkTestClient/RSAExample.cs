using Network;
using System;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// RSA example>
    /// 1. Establish a connection
    /// 2. Register Connection-Received Handlers
    /// 3. Register Packet-Handlers.
    /// 4. Send and receive a packet
    /// </summary>
    public class RSAExample
    {
#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.

        public async void Demo()
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            //1. Establish a connection.
            ClientConnectionContainer container = ConnectionFactory.CreateSecureClientConnectionContainer("127.0.0.1", 1234);
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