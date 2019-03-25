using Network;

using System;

using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// SingleConnectionExample>
    /// 1. Establish a connection to the server
    /// 2. Check whether we are connected to the server
    /// 3. Send a packet
    /// 4. Receive a packet
    /// </summary>
    public class SingleConnectionExample
    {
        public async void Demo()
        {
            ConnectionResult connectionResult = ConnectionResult.TCPConnectionNotAlive;
            //1. Establish a connection to the server.
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection("127.0.0.1", 1234, out connectionResult);
            //Because of the connectionResult we already know that we are connected.
            if (connectionResult != ConnectionResult.Connected) return;
            //2. Send and receive packets.
            CalculationResponse calculationResponse = await tcpConnection.SendAsync<CalculationResponse>(new CalculationRequest(25, 25));
            Console.WriteLine($"Answer received {calculationResponse.Result}");
        }
    }
}