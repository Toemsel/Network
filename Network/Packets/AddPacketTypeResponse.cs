using Network.Attributes;

using System.Collections.Generic;

namespace Network.Packets
{
    /// <summary>
    /// Response packet for the <see cref="AddPacketTypeRequest"/> packet.
    /// </summary>
    [PacketType(7), PacketRequest(typeof(AddPacketTypeRequest))]
    internal class AddPacketTypeResponse : ResponsePacket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddPacketTypeResponse"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="request">The request.</param>
        public AddPacketTypeResponse(List<ushort> dictionary, AddPacketTypeRequest request)
            : base(request)
        {
            LocalDict = dictionary;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// List of all the local <see cref="Packet"/> IDs that have been registered.
        /// </summary>
        public List<ushort> LocalDict { get; set; }

        #endregion Properties
    }
}