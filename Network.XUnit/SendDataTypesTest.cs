using System.Threading.Tasks;
using Network.Enums;
using Network.XUnit.Fixtures;
using Network.XUnit.Packets;
using Xunit;

namespace Network.XUnit
{
    public class SendDataTypesTest : IClassFixture<SecureServerFixture>
    {
        private SecureServerFixture serverFixture;

        public SendDataTypesTest(SecureServerFixture serverFixture) => this.serverFixture = serverFixture;

        [Fact]
        public async Task TcpSendPrimitivesTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult connectionResult);
            var simpleDataTypesResponse = await tcpConnection.SendAsync<SimpleDataTypesResponse>(new SimpleDataTypesRequest());

            Assert.Equal(ConnectionResult.Connected, connectionResult);
            Assert.Equal(PacketState.Success, simpleDataTypesResponse.State);
        }

        [Fact]
        public async Task UdpSendPrimitivesTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateSecureUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);
            var simpleDataTypesResponse = await udpConnectionh.SendAsync<SimpleDataTypesResponse>(new SimpleDataTypesRequest());

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(simpleDataTypesResponse.State, PacketState.Success);
        }

        [Fact]
        public async Task TcpSendNullablePrimitivesTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult connectionResult);
            var simpleDataTypesResponse = await tcpConnection.SendAsync<NullableSimpleDataTypesResponse>(new NullableSimpleDataTypesRequest());

            Assert.Equal(ConnectionResult.Connected, connectionResult);
            Assert.Equal(PacketState.Success, simpleDataTypesResponse.State);
        }

        [Fact]
        public async Task UdpSendNullablePrimitivesTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateSecureUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);
            var simpleDataTypesResponse = await udpConnectionh.SendAsync<NullableSimpleDataTypesResponse>(new NullableSimpleDataTypesRequest());

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(simpleDataTypesResponse.State, PacketState.Success);
        }

        [Fact]
        public async Task TcpSendObjectDataTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult connectionResult);
            var simpleDataTypesResponse = await tcpConnection.SendAsync<ObjectDataResponse>(new ObjectDataRequest());

            Assert.Equal(ConnectionResult.Connected, connectionResult);
            Assert.Equal(PacketState.Success, simpleDataTypesResponse.State);
        }

        [Fact]
        public async Task UdpSendObjectDataTest()
        {
            TcpConnection tcpConnection = ConnectionFactory.CreateSecureTcpConnection(serverFixture.Address, serverFixture.Port, out ConnectionResult tcpConnectionResult);
            UdpConnection udpConnectionh = ConnectionFactory.CreateSecureUdpConnection(tcpConnection, out ConnectionResult udpConnectionResult);
            var simpleDataTypesResponse = await udpConnectionh.SendAsync<ObjectDataResponse>(new ObjectDataRequest());

            Assert.Equal(tcpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(udpConnectionResult, ConnectionResult.Connected);
            Assert.Equal(simpleDataTypesResponse.State, PacketState.Success);
        }
    }
}