using System;
using Network.Packets;

namespace Network.XUnit.Packets
{
    public class NullableSimpleDataTypesRequest : RequestPacket
    {
        public int? Integer { get; set; } = int.MinValue;

        public uint? UnsingedInteger { get; set; } = uint.MaxValue;

        public short? Short { get; set; } = short.MaxValue;

        public ushort? UnsingedShort { get; set; } = ushort.MaxValue;

        public long? Long { get; set; } = long.MaxValue;

        public ulong? UnsingedLong { get; set; } = ulong.MaxValue;

        public double? Double { get; set; } = double.MaxValue;

        public float? Float { get; set; } = float.MaxValue;

        public bool? Boolean { get; set; } = true;

        public byte? Byte { get; set; } = byte.MaxValue;

        public char? Char { get; set; } = 'X';

        public int? IntegerNull { get; set; } = null;

        public uint? UnsingedIntegerNull { get; set; } = null;

        public short? ShortNull { get; set; } = null;

        public ushort? UnsingedShortNull { get; set; } = null;

        public long? LongNull { get; set; } = null;

        public ulong? UnsingedLongNull { get; set; } = null;

        public double? DoubleNull { get; set; } = null;

        public float? FloatNull { get; set; } = null;

        public bool? BooleanNull { get; set; } = null;

        public byte? ByteNull { get; set; } = null;

        public char? CharNull { get; set; } = null;

        //Not supported in .NET Core 2.1
        // public string? String { get; set; } = "Value";
    }
}