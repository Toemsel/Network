using Network.Attributes;
using Network.Converter;
using Network.Enums;
using Network.Interfaces;
using System;
using System.IO;

namespace Network.Packets
{
    /// <summary>
    /// Represents a packet that can be sent across a network. By default, all properties of the packet will be serialised (this can be customised using
    /// the <see cref="PacketIgnorePropertyAttribute"/>). Allowed property types are listed here: http://www.indie-dev.at/?page_id=461. NOTE: Inheriting
    /// classes should ALWAYS include the default parameter-less constructor. See 'remarks' for more information.
    /// </summary>
    /// <remarks>
    /// The default, parameter-less constructor is required to allow for dynamic instantiation of an empty packet during deserialisation, whose properties
    /// will be read from a <see cref="MemoryStream"/>, deserialised and set accordingly. See <see cref="PacketConverter"/> for more on the serialisation
    /// and deserialisation process.
    /// </remarks>
    /// <seealso cref="RequestPacket"/>
    /// <seealso cref="ResponsePacket"/>
    public abstract class Packet
    {
        #region Properties

        /// <summary>
        /// The ID of the packet. DO NOT CHANGE! This is essential to the packet recognition and handling process.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The transmission state of the packet.
        /// </summary>
        [PacketIgnoreProperty]
        public PacketState State { get; internal set; } = PacketState.Success;

        /// <summary>
        /// The size in bytes of the serialised packet.
        /// </summary>
        [PacketIgnoreProperty]
        public int Size { get; internal set; }

        /// <summary>
        /// How long it took to receive the packet, in milliseconds.
        /// </summary>
        /// <exception cref="System.NotImplementedException">This feature is not currently implemented.</exception>
        [PacketIgnoreProperty]
        public int ReceiveTime
        {
            get
            {
                throw new NotImplementedException();
            }

            internal set
            {
                throw new NotImplementedException();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// This method is called immediately before serialisation and sending. Use this to convert any properties to serialisable forms.
        /// </summary>
        public virtual void BeforeSend() { }

        /// <summary>
        /// This method is called immediately after deserialisation and before the packet is handled by the relevant <see cref="PacketReceivedHandler{T}"/>.
        /// Use this to convert any properties to their final forms.
        /// </summary>
        public virtual void BeforeReceive() { }

        #endregion Methods
    }
}