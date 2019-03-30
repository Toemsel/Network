using Network.Attributes;
using Network.Packets;

namespace Network.XUnit.Packets
{
    [PacketRequest(typeof(ObjectDataRequest))]
    public class ObjectDataResponse : ResponsePacket
    {
        public ObjectDataResponse(ObjectDataRequest request) : base(request) { }
    }
}