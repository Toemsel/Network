using Network.Attributes;
using Network.Enums;

namespace Network.Packets
{
    /// <summary>
    /// Closes the paired <see cref="Connection"/>.
    /// </summary>
    [PacketType(2)]
    internal class CloseRequest : Packet
    {
        #region Constructors

        public CloseRequest()
        {
        }

        public CloseRequest(CloseReason reason)
        {
            CloseReason = reason;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The reason that the paired <see cref="Connection"/> should close.
        /// </summary>
        public CloseReason CloseReason { get; set; }

        #endregion Properties
    }
}