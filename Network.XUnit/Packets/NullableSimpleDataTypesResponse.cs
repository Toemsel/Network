using Network.Attributes;
using Network.Packets;

namespace Network.XUnit.Packets
{
    [PacketRequest(typeof(NullableSimpleDataTypesRequest))]
    public class NullableSimpleDataTypesResponse : ResponsePacket
    {
        public NullableSimpleDataTypesResponse(NullableSimpleDataTypesRequest request) : base(request) { }
    }
}