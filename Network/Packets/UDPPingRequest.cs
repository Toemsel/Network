using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// A ping testing packet that functions over UDP.
    /// </summary>
    [PacketType(8)]
    internal class UDPPingRequest : RequestPacket
    {
    }
}