using System.Diagnostics;
using System;
using Xunit;
using Network;
using Network.Enums;
using Network.XUnit.Fixtures;

namespace Network.XUnit
{
    public class ConnectionTests : IClassFixture<ServerFixture>
    {
        private ServerFixture serverFixture;

        public ConnectionTests(ServerFixture ServerFixture) => serverFixture = ServerFixture;

        [Fact]
        public void TcpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult connectionResult);
            Assert.Equal(connectionResult, ConnectionResult.Connected);
        }

        [Fact]
        public void UdpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);
        }
    }
}
