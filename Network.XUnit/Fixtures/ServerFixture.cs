using System;
using System.Threading.Tasks;
using Network.Enums;
using Network.XUnit.Packets;

namespace Network.XUnit.Fixtures
{
    public abstract class ServerFixture<T> where T : ServerConnectionContainer
    {
        public readonly int Port;

        public readonly string Address;

        protected T ServerConnectionContainer { get; private set; }

        protected abstract ServerConnectionContainer CreateServerConnectionContainer();

        public ServerFixture()
        {
            Port = new Random().Next(1000, 50000);
            Address = "127.0.0.1";

            ServerConnectionContainer = (T)CreateServerConnectionContainer();
            ServerConnectionContainer.ConnectionEstablished += ConnectionEstablished;
            ServerConnectionContainer.AllowUDPConnections = true;
            ServerConnectionContainer.Start();
        }

        private void ConnectionEstablished(Connection connection, ConnectionType connectionType)
        {
            connection.RegisterStaticPacketHandler<SimpleDataTypesRequest>((packet, con) => con.Send(new SimpleDataTypesResponse(packet)));
            connection.RegisterStaticPacketHandler<NullableSimpleDataTypesRequest>((packet, con) => con.Send(new NullableSimpleDataTypesResponse(packet)));
            connection.RegisterStaticPacketHandler<ObjectDataRequest>((packet, con) => con.Send(new ObjectDataResponse(packet)));
        }
    }
}