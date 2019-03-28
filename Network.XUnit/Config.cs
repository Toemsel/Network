using Network;

namespace Network.XUnit
{
    public static class Config
    {
        public const int SERVER_PORT = 1234;
        public const string SERVER_ADDRESS = "127.0.0.1";

        public static ServerConnectionContainer CreateServerConnectionContainer(bool allowUdp = false)
        {
            ServerConnectionContainer serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(SERVER_PORT, false);
            serverConnectionContainer.AllowUDPConnections = allowUdp;
            return serverConnectionContainer;
        }
    }
}