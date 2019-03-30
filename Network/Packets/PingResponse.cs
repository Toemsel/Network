using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Response packet for the <see cref="PingRequest"/> packet.
    /// </summary>
    [PacketType(1)]
    internal class PingResponse : Packet { }
}