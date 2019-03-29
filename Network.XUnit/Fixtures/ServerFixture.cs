using System;
using Network.Enums;

namespace Network.XUnit.Fixtures
{
    public class ServerFixture : IDisposable
    {
        private ServerConnectionContainer serverConnectionContainer;

        public readonly int Port;

        public readonly string Address;

        public ServerFixture()
        {
            Port = new Random().Next(1000, 50000);
            Address = "127.0.0.1";

            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(Port, false);
            serverConnectionContainer.AllowUDPConnections = true;
            serverConnectionContainer.Start();
        }

        public void Dispose()
        {
            
        }
    }
}