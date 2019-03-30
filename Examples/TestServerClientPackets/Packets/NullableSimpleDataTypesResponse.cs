using Network.Attributes;
using Network.Packets;

namespace TestServerClientPackets
{
    [PacketRequest(typeof(NullableSimpleDataTypesRequest))]
    public class NullableSimpleDataTypesResponse : ResponsePacket
    {
        public NullableSimpleDataTypesResponse(NullableSimpleDataTypesRequest request) : base(request) { }
    }
}