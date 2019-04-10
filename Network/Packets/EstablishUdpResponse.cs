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

        /// <inheritdoc />
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