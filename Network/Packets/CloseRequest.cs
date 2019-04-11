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

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseRequest"/> class.
        /// </summary>
        /// <param name="reason">The reason for which the receiving <see cref="Connection"/> should close.</param>
        internal CloseRequest(CloseReason reason)
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