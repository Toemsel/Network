using System;
using Network.Enums;
using Network.RSA;

namespace Network.XUnit.Fixtures
{
    public class UnSecureServerFixture : ServerFixture<ServerConnectionContainer>
    {
        protected override ServerConnectionContainer CreateServerConnectionContainer() => ConnectionFactory.CreateServerConnectionContainer(Port, false);
    }
}