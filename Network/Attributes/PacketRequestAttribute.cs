using Network.Packets;
using System;

namespace Network.Attributes
{
    /// <summary>
    /// Maps a request packet to the response packet that handles it. This attribute should be placed on the response packet (must inherit from
    /// <see cref="ResponsePacket"/>) and the <see cref="Type"/> of the <see cref="RequestPacket"/> that it handles should be given.
    /// </summary>
    public class PacketRequestAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="PacketRequestAttribute"/> class with the given <see cref="RequestPacket"/> type as
        /// the handled <see cref="Type"/>.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/> of the <see cref="RequestPacket"/> that the decorated <see cref="ResponsePacket"/> should handle.
        /// </param>
        public PacketRequestAttribute(Type type)
        {
            RequestType = type;
        }

        #endregion Constructors

        #region Properties

        /// <summary>The <see cref="Type"/> of the <see cref="RequestPacket"/> that the <see cref="ResponsePacket"/> handles.</summary>
        public Type RequestType { get; }

        #endregion Properties
    }
}