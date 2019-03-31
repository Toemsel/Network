using Network.Packets;
using System;

namespace Network.Converter
{
    /// <summary>
    /// Describes the methods that a packet converter must implement in order to be able to serialise and deserialise
    /// packets to and from a binary form.
    /// </summary>
    public interface IPacketConverter
    {
        #region Methods

        /// <summary>
        /// Serialises the given <see cref="Packet"/> object to a <see cref="byte"/> array.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> object to serialise into a <see cref="byte"/> array.</param>
        /// <returns>A <see cref="byte"/> array that holds the serialised packet.</returns>
        byte[] SerialisePacket(Packet packet);

        /// <summary>
        /// Serialises the given <see cref="Packet"/> object to a <see cref="byte"/> array.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> object to serialise into a <see cref="byte"/> array.</param>
        /// <returns>A <see cref="byte"/> array that holds the serialised packet.</returns>
        [Obsolete("Use 'SerialisePacket' instead.")]
        byte[] GetBytes(Packet packet);

        /// <summary>
        /// Serialises the given <see cref="Packet"/> object to a <see cref="byte"/> array.
        /// </summary>
        /// <typeparam name="P">The <see cref="Type"/> of packet to serialise.</typeparam>
        /// <param name="packet">The <see cref="Packet"/> object to serialise into a <see cref="byte"/> array.</param>
        /// <returns>A <see cref="byte"/> array that holds the serialised packet.</returns>
        byte[] SerialisePacket<P>(P packet) where P : Packet;

        /// <summary>
        /// Deserialises the given <see cref="byte"/> array into a <see cref="Packet"/> of the given <see cref="Type"/>.
        /// </summary>
        /// <param name="packetType">
        /// The <see cref="Type"/> of <see cref="Packet"/> to deserialise the <see cref="byte"/> array to.
        /// </param>
        /// <param name="serialisedPacket">The <see cref="byte"/> array holding the serialised <see cref="Packet"/>.</param>
        /// <returns>The deserialised <see cref="Packet"/> object of the given type.</returns>
        Packet DeserialisePacket(Type packetType, byte[] serialisedPacket);

        /// <summary>
        /// Deserialises the given <see cref="byte"/> array into a <see cref="Packet"/> of the given <see cref="Type"/>.
        /// </summary>
        /// <param name="packetType">
        /// The <see cref="Type"/> of <see cref="Packet"/> to deserialise the <see cref="byte"/> array to.
        /// </param>
        /// <param name="serialisedPacket">The <see cref="byte"/> array holding the serialised <see cref="Packet"/>.</param>
        /// <returns>The deserialised <see cref="Packet"/> object of the given type.</returns>
        [Obsolete("Use 'DeserialisePacket' instead.")]
        Packet GetPacket(Type packetType, byte[] serialisedPacket);

        /// <summary>
        /// Deserialises the given <see cref="byte"/> array into a <see cref="Packet"/> of the given <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="P">
        /// The <see cref="Type"/> of<see cref="Packet"/> to deserialise the<see cref= "byte"/> array to.
        /// </typeparam>
        /// <param name="serialisedPacket">The <see cref="byte"/> array holding the serialised <see cref="Packet"/>.</param>
        /// <returns>The deserialised <see cref="Packet"/> object of the given type.</returns>
        P DeserialisePacket<P>(byte[] serialisedPacket) where P : Packet;

        #endregion Methods
    }
}