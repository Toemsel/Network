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

            Assert.Equal(ConnectionResult.Connected, connectionResult);

            tcpConnection.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public void SecureUdpConnectionTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateSecureUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);

            Assert.Equal(ConnectionResult.Connected, tcpConnectionResult);
            Assert.Equal(ConnectionResult.Connected, udpConnectionResult);

            tcpConnection.Close(CloseReason.ClientClosed);
            udpConnectionh.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public async Task SecureTcpConnectionAsyncTest()
        {
            var connectionResult = await ConnectionFactory.CreateSecureTcpConnectionAsync(serverFixture.Address, serverFixture.Port);

            Assert.Equal(ConnectionResult.Connected, connectionResult.Item2);

            connectionResult.Item1.Close(CloseReason.ClientClosed);
        }

        [Fact]
        public async Task SecureUdpConnectionAsyncTest()
        {
            var tcpConnectionResult = await ConnectionFactory.CreateSecureTcpConnectionAsync(serverFixture.Address, serverFixture.Port);
            var udpConnectionResult = await ConnectionFactory.CreateSecureUdpConnectionAsync(tcpConnectionResult.Item1);

            Assert.Equal(ConnectionResult.Connected, tcpConnectionResult.Item2);
            Assert.Equal(ConnectionResult.Connected, udpConnectionResult.Item2);

            tcpConnectionResult.Item1.Close(CloseReason.ClientClosed);
            udpConnectionResult.Item1.Close(CloseReason.ClientClosed);
        }
    }
}