using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Establishes a UDP connection with the paired <see cref="Connection"/>.
    /// </summary>
    [PacketType(3)]
    internal class EstablishUdpRequest : RequestPacket
    {
        #region Constructors

        public EstablishUdpRequest()
        {
        }

        public EstablishUdpRequest(int udpPort)
        {
            UdpPort = udpPort;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The port that the UDP connection should use.
        /// </summary>
        public int UdpPort { get; set; }

        #endregion Properties
    }
}