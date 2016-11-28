using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Network;

namespace NetworkTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ServerConnectionContainer connections = ConnectionFactory.CreateServerConnectionContainer(1234);

            ClientConnectionContainer clientConnection = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            clientConnection.AutoReconnect = true;

        }
    }
}
