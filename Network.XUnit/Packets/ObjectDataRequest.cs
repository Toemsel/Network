using Network.Packets;

namespace Network.XUnit.Packets
{
    public class ObjectDataRequest : RequestPacket
    {
        public Test1 Test1 { get; set; }
    }

    public class Test1
    {
        public Test2 Test2 { get; set; }

        public Test3 Test3 { get; set; }
    }

    public class Test2
    {
        public Test3 Test3 { get; set; }
    }

    public class Test3
    {
        public Test4 Test4 { get; set; }
    }

    public class Test4
    {
        public string String { get; set; }

        public int Integer { get; set; }
    }    
}