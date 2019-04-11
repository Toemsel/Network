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

        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishUdpRequest"/> class.
        /// </summary>
        /// <param name="udpPort">The port to use for UDP communication.</param>
        internal EstablishUdpRequest(int udpPort)
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