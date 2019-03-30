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

        public UDPPingResponse(UDPPingRequest request) : base(request)
        {
        }

        #endregion Constructors
    }
}