using System.Diagnostics;
using System;
using Xunit;
using Network;
using Network.Enums;

namespace Network.XUnit
{
    public class ConnectionTests : IDisposable
    {
        private ServerConnectionContainer serverConnectionContainer;

        public ConnectionTests()
        {
            serverConnectionContainer = Config.CreateServerConnectionContainer(true);
            serverConnectionContainer.Start();
        }

        [Fact]
        public void TcpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection(Config.SERVER_ADDRESS, Config.SERVER_PORT, out ConnectionResult connectionResult);
            Assert.Equal(connectionResult, ConnectionResult.Connected);
        }

        [Fact]
        public void UdpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection(Config.SERVER_ADDRESS, Config.SERVER_PORT, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);
        }

        public void Dispose()
        {
            serverConnectionContainer.CloseConnections(CloseReason.ServerClosed);
            serverConnectionContainer.Stop();
            serverConnectionContainer = null;
        }
    }
}
