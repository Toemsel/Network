using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Acknowledgement packet for the <see cref="EstablishUdpResponse"/> packet.
    /// </summary>
    [PacketType(5)]
    internal class EstablishUdpResponseACK : Packet { }
}