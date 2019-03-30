using System.Collections.Generic;
using Network.Attributes;
using Network.Packets;

namespace Network.XUnit.Packets
{
    public class SimpleDataTypesRequest : RequestPacket
    {        
        public int Integer { get; set; } = int.MinValue;

        public uint UnsingedInteger { get; set; } = uint.MaxValue;

        public short Short { get; set; } = short.MaxValue;

        public ushort UnsingedShort { get; set; } = ushort.MaxValue;

        public long Long { get; set; } = long.MaxValue;

        public ulong UnsingedLong { get; set; } = ulong.MaxValue;

        public double Double { get; set; } = double.MaxValue;

        public float Float { get; set; } = float.MaxValue;

        public bool Boolean { get; set; } = true;

        public byte Byte { get; set; } = byte.MaxValue;

        public char Char { get; set; } = 'X';

        public string String { get; set; } = "Value";
    }
}