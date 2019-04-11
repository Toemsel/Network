using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Response to a <see cref="UDPPingRequest"/> packet.
    /// </summary>
    [PacketType(9), PacketRequest(typeof(UDPPingRequest))]
    internal class UDPPingResponse : ResponsePacket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UDPPingRequest"/> class.
        /// </summary>
        /// <param name="request">The handled <see cref="UDPPingRequest"/>.</param>
        internal UDPPingResponse(UDPPingRequest request) : base(request)
        {
        }

        #endregion Constructors
    }
}