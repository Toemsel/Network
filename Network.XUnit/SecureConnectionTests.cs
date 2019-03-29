using System.Threading.Tasks;
using Network.Enums;
using Network.XUnit.Fixtures;
using Xunit;

namespace Network.XUnit
{
    public class SecureConnectionTests : IClassFixture<SecureServerFixture>
    {
        private SecureServerFixture serverFixture;

        public SecureConnectionTests(SecureServerFixture ServerFixture) => serverFixture = ServerFixture;

        [Fact]
        public void SecureTcpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult connectionResult);

            Assert.Equal(connectionResult, ConnectionResult.Connected);

            tcpConnection.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public void SecureUdpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateSecureUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);

            tcpConnection.Close(CloseReason.ClientClosed);
            udpConnectionh.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public async Task SecureTcpConnectionAsyncTest()
        {
            var connectionResult = await ConnectionFactory.CreateSecureTcpConnectionAsync(serverFixture.Address, serverFixture.Port);

            Assert.Equal(connectionResult.Item2, ConnectionResult.Connected);

            connectionResult.Item1.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public async Task SecureUdpConnectionAsyncTest()
        {
            var tcpConnectionResult = await ConnectionFactory.CreateSecureTcpConnectionAsync(serverFixture.Address, serverFixture.Port);
            var udpConnectionResult = await ConnectionFactory.CreateSecureUdpConnectionAsync(tcpConnectionResult.Item1);

            Assert.Equal(tcpConnectionResult.Item2, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult.Item2, ConnectionResult.Connected);

            tcpConnectionResult.Item1.Close(CloseReason.ClientClosed);
            udpConnectionResult.Item1.Close(CloseReason.ClientClosed);
        }
    }
}