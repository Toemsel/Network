using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Used to perform ping checks between <see cref="Connection"/>s.
    /// </summary>
    [PacketType(0)]
    internal class PingRequest : Packet { }
}