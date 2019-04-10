using System;

namespace Network.Converter
{
    /// <summary>
    /// Enumerates the possible states of a network object before and after deserialisation.
    /// </summary>
    public enum ObjectState : byte
    {
        /// <summary>
        /// The network object is null, so there is nothing to read from the network stream.
        /// </summary>
        Null = 0x00,

        /// <summary>
        /// Identical to <see cref="Null"/>.
        /// </summary>
        [Obsolete("Use 'Null' instead.")]
        NULL = Null,

        /// <summary>
        /// The network object is not null, so there is something to read from the network stream.
        /// </summary>
        NotNull = 0xFF,

        /// <summary>
        /// Identical to <see cref="NotNull"/>.
        /// </summary>
        [Obsolete("Use 'NotNull' instead.")]
        NOT_NULL = NotNull,
    }
}