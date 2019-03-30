using Network.Attributes;
using Network.Packets;

namespace Network.XUnit.Packets
{
    [PacketRequest(typeof(SimpleDataTypesRequest))]
    public class SimpleDataTypesResponse : ResponsePacket
    {
        public SimpleDataTypesResponse(SimpleDataTypesRequest request) : base(request) { }
    }
}