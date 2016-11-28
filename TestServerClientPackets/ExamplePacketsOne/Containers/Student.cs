using System.Collections.Generic;
using Network.Attributes;

namespace TestServerClientPackets.ExamplePacketsOne.Containers
{
    public class Student
    {
        public string FirstName { get; set; }

        public string Lastname { get; set; }

        [PacketIgnoreProperty]
        public string FullName { get { return FirstName + " " + Lastname; } }
        
        public Date Birthday { get; set; }

        public List<GeoCoordinate> VisitedPlaces { get; set; } = new List<GeoCoordinate>();
    }
}
