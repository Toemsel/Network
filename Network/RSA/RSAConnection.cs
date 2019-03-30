using Network.Converter;
using Network.Interfaces;
using Network.Packets;
using Network.Packets.RSA;
using System;
using System.Security.Cryptography;

namespace Network.RSA
{
    /// <summary>
    /// Provides RSA encryption and decryption facilities to a <see cref="Network.Connection"/>,
    /// allowing encrypted communication.
    /// </summary>
    /// <seealso cref="IRSACapability" />
    /// <seealso cref="IRSAConnection" />
    internal class RSAConnection : IPacketConverter, IRSACapability, IRSAConnection
    {
        #region Variables

        /// <summary>
        /// The <see cref="Network.RSA.RSAPair"/> for the remote communication
        /// partner.
        /// </summary>
        private volatile RSAPair remoteRSAKeyPair;

        /// <summary>
        /// The RSA encryption provider for encrypting packets.
        /// </summary>
        private volatile RSACryptoServiceProvider encryptionProvider;

        /// <summary>
        /// Whether RSA encryption is active on this connection.
        /// </summary>
        private volatile bool isRSACommunicationActive = false;

        #endregion Variables

        #region Constructors

        public RSAConnection(Connection connection, RSAPair rsaPair)
        {
            Connection = connection;
            RSAPair = rsaPair;

            DecryptionProvider = new RSACryptoServiceProvider(RSAPair.KeySize);
            Extensions.RSACryptoServiceProviderExtensions.ImportParametersFromXmlString(DecryptionProvider, RSAPair.Private);

            //Are we running on WinXP or higher?
            OperatingSystem operatingSystem = Environment.OSVersion;
            XPOrHigher = (operatingSystem.Platform == PlatformID.Win32NT) &&
                ((operatingSystem.Version.Major > 5) || ((operatingSystem.Version.Major == 5) &&
                (operatingSystem.Version.Minor >= 1)));

            //Setup RSA related packets.
            ExchangePublicKeys();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The underlying <see cref="Network.Connection"/> object that allows
        /// communication across the network.
        /// </summary>
        private Connection Connection { get; set; }

        /// <summary>
        /// Use your own packetConverter to serialize/deserialze objects.
        /// Take care that the internal packet structure should still remain the same:
        ///     1. [16bits]  packet type
        ///     2. [32bits]  packet length
        ///     3. [xxbits]  packet serialisedPacket
        /// The default packetConverter uses reflection to get and set serialisedPacket within objects.
        /// Using your own packetConverter could result in a higher throughput.
        /// </summary>
        public IPacketConverter PacketConverter { get; set; }

        /// <summary>
        /// Gets or sets the RSA pair.
        /// </summary>
        /// <value>The RSA pair.</value>
        public RSAPair RSAPair { get; set; }

        /// <summary>
        /// Is this application running on windowsXP or higher?
        /// </summary>
        public bool XPOrHigher { get; private set; }

        /// <summary>
        /// Gets or sets the communication partner's RSA pair.
        /// </summary>
        /// <value>The communication partner RSA pair.</value>
        public RSAPair CommunicationPartnerRSAPair
        {
            get => remoteRSAKeyPair;
            set => remoteRSAKeyPair = value;
        }

        /// <summary>
        /// Gets or sets the encryption provider.
        /// </summary>
        /// <value>The encryption provider.</value>
        public RSACryptoServiceProvider EncryptionProvider
        {
            get => encryptionProvider;
            set => encryptionProvider = value;
        }

        /// <summary>
        /// Gets or sets the decryption provider.
        /// </summary>
        /// <value>The decryption provider.</value>
        public RSACryptoServiceProvider DecryptionProvider { get; set; }

        /// <summary>
        /// Indicates if the RSA en/decryption is active.
        /// RSA encryption requires some an initialization process,
        /// thus won't be available instantly after the connection has
        /// been established. Once [True] (active) it won't toggle.
        /// </summary>
        /// <value><c>true</c> if RSA is active; otherwise, <c>false</c>.</value>
        public bool IsRSACommunicationActive
        {
            get => isRSACommunicationActive && (CommunicationPartnerRSAPair?.HasPublicKey ?? false);
            private set => isRSACommunicationActive = value;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes the RSA communication serialisedPacket.
        /// Sends our information to the communication partner.
        /// Subscribes to the RSA packet events.
        /// </summary>
        private async void ExchangePublicKeys()
        {
            Connection.RegisterStaticPacketHandler<RSAKeyInformationRequest>((rsaKeyRequest, connection) =>
            {
                connection.DeregisterStaticPacketHandler<RSAKeyInformationRequest>();

                CommunicationPartnerRSAPair = new RSAPair(rsaKeyRequest.PublicKey, rsaKeyRequest.KeySize);
                EncryptionProvider = new RSACryptoServiceProvider(CommunicationPartnerRSAPair.KeySize);
                Extensions.RSACryptoServiceProviderExtensions.ImportParametersFromXmlString(EncryptionProvider, CommunicationPartnerRSAPair.Public);

                connection.Send(new RSAKeyInformationResponse(RSAPair.Public, RSAPair.KeySize, rsaKeyRequest));
            });

            RSAKeyInformationResponse keyInformationResponse = await Connection.SendAsync<RSAKeyInformationResponse>(new RSAKeyInformationRequest(RSAPair.Public, RSAPair.KeySize)).ConfigureAwait(false);

            Connection.Logger.Log($"{Connection.GetType().Name} RSA Encryption active.", Enums.LogLevel.Information);
            IsRSACommunicationActive = true;
        }

        /// <summary>
        /// Decrypt bytes with the <see cref="T:System.Security.Cryptography.RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The bytes to decrypt.</param>
        /// <returns>The decrypted bytes.</returns>
        public byte[] DecryptBytes(byte[] bytes) => DecryptionProvider.Decrypt(bytes, XPOrHigher);

        /// <summary>
        /// Encrypts bytes with the <see cref="T:System.Security.Cryptography.RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The Bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        public byte[] EncryptBytes(byte[] bytes) => EncryptionProvider.Encrypt(bytes, XPOrHigher);

        #region Implementation of IPacketConverter

        /// <summary>
        /// Serialises the given <see cref="Packet"/>, and encrypts the resulting
        /// bytes using the private RSA key and the <see cref="EncryptionProvider"/>.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> object to serialise.
        /// </param>
        /// <returns>
        /// The RSA encrypted bytes that represent the given packet.
        /// </returns>
        public byte[] SerialisePacket(Packet packet)
        {
            bool isRSACommunicationActive = IsRSACommunicationActive;
            byte[] unEncryptedData = PacketConverter.SerialisePacket(packet);

            byte[] rsaStatus = BitConverter.GetBytes(isRSACommunicationActive);
            byte[] packetData = isRSACommunicationActive ? EncryptBytes(unEncryptedData) : unEncryptedData;
            byte[] packetDataWithStatus = new byte[packetData.Length + 1];
            Array.Copy(rsaStatus, 0, packetDataWithStatus, 0, 1);
            Array.Copy(packetData, 0, packetDataWithStatus, 1, packetData.Length);
            return packetDataWithStatus;
        }

        /// <summary>
        /// Serialises the given <see cref="Packet"/>, and encrypts the resulting
        /// bytes using the private RSA key and the <see cref="EncryptionProvider"/>.
        /// </summary>
        /// <param name="packet">
        /// The <see cref="Packet"/> object to serialise.
        /// </param>
        /// <returns>
        /// The RSA encrypted bytes that represent the given packet.
        /// </returns>
        [Obsolete("Use 'SerialisePacket' instead.")]
        public byte[] GetBytes(Packet packet)
        {
            return SerialisePacket(packet);
        }

        /// <summary>
        /// Serialises the given <see cref="Packet"/>, and encrypts the resulting
        /// bytes using the private RSA key and the <see cref="EncryptionProvider"/>.
        /// </summary>
        /// <typeparam name="P">
        /// The type of the packet to serialise.
        /// </typeparam>
        /// <param name="packet">
        /// The <see cref="Packet"/> object to serialise.
        /// </param>
        /// <returns>
        /// The RSA encrypted bytes that represent the given packet.
        /// </returns>
        public byte[] SerialisePacket<P>(P packet) where P : Packet
        {
            bool isRSACommunicationActive = IsRSACommunicationActive;
            byte[] unEncryptedData = PacketConverter.SerialisePacket(packet);

            byte[] rsaStatus = BitConverter.GetBytes(isRSACommunicationActive);
            byte[] packetData = isRSACommunicationActive ? EncryptBytes(unEncryptedData) : unEncryptedData;
            byte[] packetDataWithStatus = new byte[packetData.Length + 1];
            Array.Copy(rsaStatus, 0, packetDataWithStatus, 0, 1);
            Array.Copy(packetData, 0, packetDataWithStatus, 1, packetData.Length);
            return packetDataWithStatus;
        }

        /// <summary>
        /// Deserialises the given encrypted bytes into a <see cref="Packet"/>
        /// of the given type.
        /// </summary>
        /// <param name="packetType">
        /// The type of packet to deserialise the bytes to.
        /// </param>
        /// <param name="serialisedPacket">
        /// The RSA encrypted bytes to deserialise.
        /// </param>
        /// <returns>
        /// The deserialised <see cref="Packet"/> object.
        /// </returns>
        public Packet DeserialisePacket(Type packetType, byte[] serialisedPacket)
        {
            bool isRSACommunicationActive = serialisedPacket[0] == 1;
            byte[] data = new byte[serialisedPacket.Length - 1];
            Array.Copy(serialisedPacket, 1, data, 0, data.Length);

            return isRSACommunicationActive ? PacketConverter.DeserialisePacket(packetType, DecryptBytes(data))
                : PacketConverter.DeserialisePacket(packetType, data);
        }

        /// <summary>
        /// Deserialises the given encrypted bytes into a <see cref="Packet"/>
        /// of the given type.
        /// </summary>
        /// <param name="packetType">
        /// The type of packet to deserialise the bytes to.
        /// </param>
        /// <param name="serialisedPacket">
        /// The RSA encrypted bytes to deserialise.
        /// </param>
        /// <returns>
        /// The deserialised <see cref="Packet"/> object.
        /// </returns>
        [Obsolete("Use 'DeserialisePacket' instead.")]
        public Packet GetPacket(Type packetType, byte[] serialisedPacket)
        {
            return DeserialisePacket(packetType, serialisedPacket);
        }

        /// <summary>
        /// Deserialises the given encrypted bytes into a <see cref="Packet"/>
        /// of the given type.
        /// </summary>
        /// <typeparam name="P">
        /// The type of packet to deserialise the bytes to.
        /// </typeparam>
        /// <param name="serialisedPacket">
        /// The RSA encrypted bytes to deserialise.
        /// </param>
        /// <returns>
        /// The deserialised <see cref="Packet"/> object.
        /// </returns>
        public P DeserialisePacket<P>(byte[] serialisedPacket) where P : Packet
        {
            bool isRSACommunicationActive = serialisedPacket[0] == 1;
            byte[] data = new byte[serialisedPacket.Length - 1];
            Array.Copy(serialisedPacket, 1, data, 0, data.Length);

            return isRSACommunicationActive ?
                PacketConverter.DeserialisePacket<P>(DecryptBytes(data))
                : PacketConverter.DeserialisePacket<P>(data);
        }

        #endregion Implementation of IPacketConverter

        #endregion Methods
    }
}