using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Response packet for the <see cref="EstablishUdpRequest"/> packet.
    /// </summary>
    [PacketType(4), PacketRequest(typeof(EstablishUdpRequest))]
    internal class EstablishUdpResponse : ResponsePacket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EstablishUdpResponse"/> class.
        /// </summary>
        /// <param name="udpPort">The port to use for UDP communication.</param>
        /// <param name="request">The handled <see cref="EstablishUdpRequest"/>.</param>
        internal EstablishUdpResponse(int udpPort, RequestPacket request) : base(request)
        {
            UdpPort = udpPort;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The port the UDP connection should use.
        /// </summary>
        public int UdpPort { get; set; }

        #endregion Properties
    }
}