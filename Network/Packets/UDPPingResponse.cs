using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Represents a UDP Ping response.
    /// </summary>
    [PacketType(9)]
    [PacketRequest(typeof(UDPPingRequest))]
    internal class UDPPingResponse : ResponsePacket
    {
        public UDPPingResponse(UDPPingRequest request)
            : base(request) { }
    }
}