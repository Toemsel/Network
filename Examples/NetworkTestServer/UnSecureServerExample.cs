using Network;
using Network.Enums;
using Network.Extensions;
using System;
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
    public class UnSecureServerExample
    {
        private ServerConnectionContainer serverConnectionContainer;

        public void Demo()
        {
            //1. Start listen on a port
            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(1234, false);

            //2. Apply optional settings.
            #region Optional settings

            serverConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{serverConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            serverConnectionContainer.ConnectionEstablished += connectionEstablished;
#if NET46
            serverConnectionContainer.AllowBluetoothConnections = true;
#endif
            serverConnectionContainer.AllowUDPConnections = true;
            serverConnectionContainer.UDPConnectionLimit = 2;

            #endregion Optional settings

            //Call start here, because we had to enable the bluetooth property at first.
            serverConnectionContainer.Start();

            //Don't close the application.
            Console.ReadLine();
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
            connection.RegisterStaticPacketHandler<NullableSimpleDataTypesRequest>((p, c) => c.Send(new NullableSimpleDataTypesResponse(p)));
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
            connection.Send(new AddStudentToDatabaseResponse(DatabaseResult.Success, packet));
        }
    }
}