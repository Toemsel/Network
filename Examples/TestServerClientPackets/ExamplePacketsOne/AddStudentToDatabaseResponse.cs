using Network.Attributes;
using Network.Packets;
using TestServerClientPackets.ExamplePacketsOne.Containers;

namespace TestServerClientPackets.ExamplePacketsOne
{
    [PacketRequest(typeof(AddStudentToDatabaseRequest))]
    public class AddStudentToDatabaseResponse : ResponsePacket
    {
        public AddStudentToDatabaseResponse(DatabaseResult result,
            AddStudentToDatabaseRequest addStudentRequest)
            : base(addStudentRequest)
        {
            Result = result;
        }

        public DatabaseResult Result { get; set; }
    }
}
