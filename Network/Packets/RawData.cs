using Network.Attributes;

namespace Network.Packets
{
    /// <summary>
    /// Sends a raw, primitive value across a network.
    /// </summary>
    [PacketType(10)]
    public class RawData : Packet
    {
        #region Properties

        /// <summary>
        /// The key both connections are able to register <see cref="RawData"/>
        /// packet handlers to.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The serialised primitive value.
        /// </summary>
        public byte[] Data { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Parameter-less constructor for packet instantiation, used during
        /// serialisation and deserialisation.
        /// </summary>
        public RawData()
        {
        }

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="RawData"/>
        /// packet, with the given key and data.
        /// </summary>
        /// <param name="key">
        /// The key that <see cref="RawData"/> packet handlers are registered with.
        /// </param>
        /// <param name="data">
        /// The serialised primitive value.
        /// </param>
        public RawData(string key, byte[] data)
        {
            Key = key;
            Data = data;
        }

        #endregion Constructors
    }
}