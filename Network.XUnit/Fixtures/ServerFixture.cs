using System;
namespace Network.XUnit.Fixtures
{
    public abstract class ServerFixture<T> : IDisposable where T : ServerConnectionContainer
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
            ServerConnectionContainer.AllowUDPConnections = true;
            ServerConnectionContainer.Start();
        }

        public void Dispose() { }
    }
}