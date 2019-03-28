using Network.Packets;

namespace Network.Enums
{
    /// <summary>
    /// Enumerates the possible states that a <see cref="Packet"/> could be in
    /// after transmission.
    /// </summary>
    public enum PacketState
    {
        /// <summary>
        /// The packet was successfully transmitted and received.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The packet was not received within the specified timeout. The
        /// <see cref="Connection"/> could be dead.
        /// </summary>
        Timeout = 1,

        /// <summary>
        /// The <see cref="Connection"/> is not alive, so no asynchronous
        /// transmission is possible.
        /// </summary>
        ConnectionNotAlive = 2
    }
}