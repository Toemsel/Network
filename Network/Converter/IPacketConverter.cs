using Network.Packets;
using System;

namespace Network.Converter
{
    /// <summary>
    /// Describes the methods that a packet converter must implement in order
    /// to be able to serialise and deserialise packets to and from a binary
    /// form.
    /// </summary>
    public interface IPacketConverter
    {
        #region Methods

        /// <summary>
        /// Serialises a given packet to a byte array.
        /// </summary>
        /// <param name="packet">The packet to convert.</param>
        /// <returns>System.Byte[].</returns>
        byte[] SerialisePacket(Packet packet);

        /// <summary>
        /// Serialises the given packet of the given type to a byte array.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet to serialise into a byte array.
        /// </typeparam>
        /// <param name="packet">
        /// The packet object to serialise into a byte array.
        /// </param>
        /// <returns>
        /// An array of <see cref="byte"/>s that holds the serialised packet.
        /// </returns>
        byte[] SerialisePacket<P>(P packet) where P : Packet;

        /// <summary>
        /// Deserialises the given data byte array into an object of the given
        /// type.
        /// </summary>
        /// <param name="packetType">
        /// The type of object to deserialise the byte array to.
        /// </param>
        /// <param name="serialisedPacket">
        /// The byte array holding the serialised packet.
        /// </param>
        /// <returns>
        /// The deserialised packet object of the given type.
        /// </returns>
        Packet DeserialisePacket(Type packetType, byte[] serialisedPacket);

        /// <summary>
        /// Deserialises the given byte array into an object of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type to which to deserialise the given byte array.
        /// </typeparam>
        /// <param name="serialisedPacket">
        /// The byte array holding the serialised packet.
        /// </param>
        /// <returns>
        /// The deserialised packet object, of the given type.
        /// </returns>
        P DeserialisePacket<P>(byte[] serialisedPacket) where P : Packet;

        #endregion Methods
    }
}