namespace Network.Converter
{
    /// <summary>
    /// The possible states of a network object.
    /// </summary>
    public enum ObjectState : byte
    {
        /// <summary>
        /// The object is null.
        /// We didn't write something on the stream.
        /// So we cant read anything from the stream.
        /// </summary>
        NULL = 0x00,
        /// <summary>
        /// The object is not null.
        /// We wrote something on the stream.
        /// So we can read something from the stream.
        /// </summary>
        NOT_NULL = 0xFF
    }
}
