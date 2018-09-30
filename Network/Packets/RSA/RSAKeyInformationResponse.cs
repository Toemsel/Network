using Network.Attributes;

namespace Network.Packets.RSA
{
    [PacketType(22)]
    [PacketRequest(typeof(RSAKeyInformationRequest))]
    internal class RSAKeyInformationResponse : ResponsePacket
    {
        public RSAKeyInformationResponse() { }

        public RSAKeyInformationResponse(string publicKey, int keySize, RSAKeyInformationRequest request)
            : base(request)
        {
            PublicKey = publicKey;
            KeySize = keySize;
        }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        /// <value>The public key.</value>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the size of the key.
        /// </summary>
        /// <value>The size of the key.</value>
        public int KeySize { get; set; }
    }
}
