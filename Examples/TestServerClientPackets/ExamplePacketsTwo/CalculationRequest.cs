using System.Collections.Generic;
using Network.Packets;

namespace TestServerClientPackets
{
    public class CalculationRequest : RequestPacket
    {
        public CalculationRequest(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public int X { get; set; }

        public int Y { get; set; }
    }
}
