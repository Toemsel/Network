using Network.Packets;
using System;

namespace Network.Interfaces
{
    /// <summary>
    /// Represents a method that handles receiving a <see cref="Packet"/> of
    /// the given type on the given <see cref="Connection"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of <see cref="Packet"/> that the delegate should handle.
    /// </typeparam>
    /// <param name="packet">
    /// The received <see cref="Packet"/> object.
    /// </param>
    /// <param name="connection">
    /// The <see cref="Connection"/> that received the packet.
    /// </param>
    public delegate void PacketReceivedHandler<T>(T packet, Connection connection) where T : Packet;

    /// <summary>
    /// Describes the methods a class must implement to handle <see cref="Packet"/>s.
    /// </summary>
    public interface IPacketHandler
    {
        #region Methods

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{T}"/> for all
        /// <see cref="Packet"/>s of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> the delegate should handle.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="PacketReceivedHandler{T}"/> delegate to be invoked
        /// for each received packet of the given type.
        /// </param>
        void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet;

        /// <summary>
        /// Registers the given <see cref="PacketReceivedHandler{T}"/> on the
        /// given <see cref="object"/> for all <see cref="Packet"/>s of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> the delegate should handle.
        /// </typeparam>
        /// <param name="handler">
        /// The <see cref="PacketReceivedHandler{T}"/> delegate to be invoked
        /// for each received packet of the given type.
        /// </param>
        /// <param name="obj">
        /// The <see cref="object"/> that should receive the <see cref="Packet"/>s.
        /// </param>
        void RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet;

        /// <summary>
        /// Deregisters all <see cref="PacketReceivedHandler{T}"/>s for the given
        /// <see cref="Packet"/> type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> for which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s should be deregistered.
        /// </typeparam>
        void UnRegisterStaticPacketHandler<P>() where P : Packet;

        /// <summary>
        /// Deregisters all <see cref="PacketReceivedHandler{T}"/>s for the given
        /// <see cref="Packet"/> type that are currently registered on the given
        /// <see cref="object"/>.
        /// </summary>
        /// <typeparam name="P">
        /// The type of <see cref="Packet"/> for which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s should be deregistered.
        /// </typeparam>
        /// <param name="obj">
        /// The <see cref="object"/> on which all currently registered
        /// <see cref="PacketReceivedHandler{T}"/>s of the given type should
        /// be deregistered.
        /// </param>
        void UnRegisterPacketHandler<P>(object obj) where P : Packet;

        #endregion Methods
    }
}