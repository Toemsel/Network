using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// A UDP PingRequest packet.
    /// Used to test the latency between server and client.
    /// </summary>
    [PacketType(10)]
    internal class UDPPingRequest : RequestPacket
    {
        public UDPPingRequest() { }
    }
}
