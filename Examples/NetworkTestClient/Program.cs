using System;
using System.Text;
using Network;
using Network.Converter;
using Network.Enums;
using Network.Extensions;

namespace NetworkTestClient
{
    public static class Program
    {
            static void Main(string[] args)
            {
                RunServer();
                RunClient();
                Console.ReadLine();
            }

            public static void RunClient()
            {
                ConnectionResult connectionResult = ConnectionResult.TCPConnectionNotAlive;
                //1. Establish a connection to the server.
                var tcpConnection = ConnectionFactory.CreateSecureClientConnectionContainer("127.0.0.1", 1234);
                tcpConnection.ConnectionEstablished += (c, b) =>
                {
                        Console.WriteLine($"{tcpConnection.ToString()} Connection established");

                        //3. Send a raw data packet request.
                        c.SendRawData(RawDataConverter.FromUTF8String("HelloWorld", "Hello, this is the RawDataExample!"));
                        c.SendRawData(RawDataConverter.FromBoolean("BoolValue", true));
                        c.SendRawData(RawDataConverter.FromBoolean("BoolValue", false));
                        c.SendRawData(RawDataConverter.FromDouble("DoubleValue", 32.99311325d));
                        //4. Send a raw data packet request without any helper class
                        c.SendRawData("HelloWorld", Encoding.UTF8.GetBytes("Hello, this is the RawDataExample!"));
                        c.SendRawData("BoolValue", BitConverter.GetBytes(true));
                        c.SendRawData("BoolValue", BitConverter.GetBytes(false));
                        c.SendRawData("DoubleValue", BitConverter.GetBytes(32.99311325d));

                };
            }

            public static void RunServer()
            {
                //1. Start listen on a port
                var serverConnectionContainer = ConnectionFactory.CreateSecureServerConnectionContainer(1234, start: false);

                //2. Apply optional settings.
                serverConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{serverConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
                serverConnectionContainer.ConnectionEstablished += connectionEstablished;
                // serverConnectionContainer.AllowBluetoothConnections = false;
                serverConnectionContainer.AllowUDPConnections = true;
                serverConnectionContainer.UDPConnectionLimit = 2;

                serverConnectionContainer.Start();
            }

            /// <summary>
            /// We got a connection.
            /// </summary>
            /// <param name="connection">The connection we got. (TCP or UDP)</param>
            private static void connectionEstablished(Connection connection, ConnectionType type)
            {
                //C//onsole.WriteLine($"{serverConnectionContainer.Count} {connection.GetType()} connected on port {connection.IPRemoteEndPoint.Port}");

                //3. Register packet listeners.
                connection.RegisterRawDataHandler("HelloWorld", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToUTF8String()}"));
                connection.RegisterRawDataHandler("BoolValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToBoolean()}"));
                connection.RegisterRawDataHandler("DoubleValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToDouble()}"));
            }
        }
}