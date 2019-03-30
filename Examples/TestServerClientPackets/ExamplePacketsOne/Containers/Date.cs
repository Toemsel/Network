using Network.Attributes;
using System;

namespace TestServerClientPackets.ExamplePacketsOne.Containers
{
    public class Date
    {
        public int Day { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [PacketIgnoreProperty]
        public DateTime DateTime { get { return new DateTime(Year, Month, Day); } }
    }
}