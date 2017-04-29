using Network;
using Network.Attributes;
using Network.Packets;

namespace TestServerClientPackets
{
    [PacketRequest(typeof(CalculationRequest))]
    public class CalculationResponse : ResponsePacket
    {
        public CalculationResponse()
        {

        }

        public CalculationResponse(int Result, RequestPacket request)
            : base(request)
        {
            this.Result = Result;
        }

        public int Result { get; set; }
    }
}
