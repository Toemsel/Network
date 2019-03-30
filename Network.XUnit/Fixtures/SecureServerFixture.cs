using Network.RSA;

namespace Network.XUnit.Fixtures
{
    public class SecureServerFixture : ServerFixture<SecureServerConnectionContainer>
    {
        protected override ServerConnectionContainer CreateServerConnectionContainer() => ConnectionFactory.CreateSecureServerConnectionContainer(Port, start: false);
    }
}