using System;
using System.Text;
using Network;
using Network.Converter;

namespace NetworkTestClient
{
    /// <summary>
    /// Simple example>
    /// 1. Establish a connection
    /// 2. Subscribe connectionEstablished event
    /// 3. Send a raw data packet with the helper classes
    /// 4. Send a raw data packet without the helper classes
    /// </summary>
    public class RawDataExample
    {
        public async void Demo()
        {
            ConnectionResult connectionResult = ConnectionResult.TCPConnectionNotAlive;
            //1. Establish a connection to the server.
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection("127.0.0.1", 1234, out connectionResult);
            //2. Register what happens if we get a connection
            if(connectionResult == ConnectionResult.Connected)
            {
                Console.WriteLine($"{tcpConnection.ToString()} Connection established");
                //3. Send a raw data packet request.
                tcpConnection.SendRawData(RawDataConverter.FromUTF8String("HelloWorld", "Hello, this is the RawDataExample!"));
                tcpConnection.SendRawData(RawDataConverter.FromBoolean("BoolValue", true));
                tcpConnection.SendRawData(RawDataConverter.FromBoolean("BoolValue", false));
                tcpConnection.SendRawData(RawDataConverter.FromDouble("DoubleValue", 32.99311325d));
                //4. Send a raw data packet request without any helper class
                tcpConnection.SendRawData("HelloWorld", Encoding.UTF8.GetBytes("Hello, this is the RawDataExample!"));
                tcpConnection.SendRawData("BoolValue", BitConverter.GetBytes(true));
                tcpConnection.SendRawData("BoolValue", BitConverter.GetBytes(false));
                tcpConnection.SendRawData("DoubleValue", BitConverter.GetBytes(32.99311325d));
            }
        }
    }
}
