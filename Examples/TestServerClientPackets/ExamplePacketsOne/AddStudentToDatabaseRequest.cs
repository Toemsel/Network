using System.Collections.Generic;
using Network.Packets;
using TestServerClientPackets.ExamplePacketsOne.Containers;

namespace TestServerClientPackets.ExamplePacketsOne
{
    public class AddStudentToDatabaseRequest : RequestPacket
    {
        public AddStudentToDatabaseRequest()
        {

        }

        public AddStudentToDatabaseRequest(Student student)
        {
            Student = student;
        }

        public Student Student { get; set; }

        public List<string> Rooms { get; set; }
    }
}
