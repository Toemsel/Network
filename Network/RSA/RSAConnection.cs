using System.Linq;
using Network.Converter;
using Network.Interfaces;
using Network.Packets;
using Network.Packets.RSA;
using System;
using System.Collections.Generic;
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
        /// Holds the RSA key-pair for the remote <see cref="Network.Connection"/>.
        /// </summary>
        private volatile RSAPair remoteRSAKeyPair;

        /// <summary>
        /// The RSA encryption provider for encrypting and decrypting packets.
        /// </summary>
        private volatile RSACryptoServiceProvider encryptionProvider;

        /// <summary>
        /// Whether RSA encryption is currently active on this connection.
        /// </summary>
        private volatile bool isRSACommunicationActive = false;

        /// <summary>
        /// RSA is only capable of encrypting byte array of this size.
        /// The max encryption size isn't abitrary; but indirectly set by
        /// the <see chref="RSACryptoServiceProvider" />
        /// </summary>
        private const int MAX_ENCRYPTION_BYTE_SIZE = 128;
        
        /// <summary>
        /// RSA is only capable of encrypting byte array of this size.
        /// The max encryption size isn't abitrary; but indirectly set by
        /// the <see chref="RSACryptoServiceProvider" />
        /// </summary>
        private const int MAX_DECRYPTION_BYTE_SIZE = 256;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RSAConnection"/> class.
        /// </summary>
        /// <param name="connection">The base <see cref="Network.Connection"/> for sending and receiving data.</param>
        /// <param name="rsaPair">The local RSA key-pair for this <see cref="RSAConnection"/>.</param>
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
        /// The underlying <see cref="Network.Connection"/> object that allows communication across the network.
        /// </summary>
        private Connection Connection { get; set; }

        /// <summary>
        /// Allows the usage of a custom <see cref="IPacketConverter"/> implementation for serialisation and deserialisation.
        /// However, the internal structure of the packet should stay the same:
        ///     Packet Type     : 2  bytes (ushort)
        ///     Packet Length   : 4  bytes (int)
        ///     Packet Data     : xx bytes (actual serialised packet data)
        /// </summary>
        /// <remarks>
        /// The default <see cref="PacketConverter"/> uses reflection (with type property caching) for serialisation
        /// and deserialisation. This allows good performance over the widest range of packets. Should you want to
        /// handle only a specific set of packets, a custom <see cref="IPacketConverter"/> can allow more throughput (no slowdowns
        /// due to relatively slow reflection).
        /// </remarks>
        public IPacketConverter PacketConverter { get; set; }

        /// <summary>
        /// The local RSA key-pair for encryption, decryption, and signing.
        /// </summary>
        public RSAPair RSAPair { get; set; }

        /// <summary>
        /// Whether the <see cref="RSAConnection"/> is running on Windows XP or higher.
        /// </summary>
        public bool XPOrHigher { get; private set; }

        /// <summary>
        /// The remote RSA key-pair.
        /// </summary>
        public RSAPair CommunicationPartnerRSAPair
        {
            get => remoteRSAKeyPair;
            set => remoteRSAKeyPair = value;
        }

        /// <summary>
        /// The <see cref="RSACryptoServiceProvider"/> to use for encryption.
        /// </summary>
        public RSACryptoServiceProvider EncryptionProvider
        {
            get => encryptionProvider;
            set => encryptionProvider = value;
        }

        /// <summary>
        /// The <see cref="RSACryptoServiceProvider"/> to use for decryption.
        /// </summary>
        public RSACryptoServiceProvider DecryptionProvider { get; set; }

        /// <summary>
        /// Whether the RSA functionality is active. RSA functionality requires an additional initialization process, thus
        /// won't be available immediately after the connection has been established. It will never revert to <c>false</c>
        /// once set.
        /// </summary>
        public bool IsRSACommunicationActive
        {
            get => isRSACommunicationActive && (CommunicationPartnerRSAPair?.HasPublicKey ?? false);
            private set => isRSACommunicationActive = value;
        }

        #endregion Properties

        #region Methods

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
        public byte[] GetBytes(Packet packet)
        {
            bool isRSACommunicationActive = IsRSACommunicationActive;
            byte[] unEncryptedData = PacketConverter.GetBytes(packet);

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
        /// <typeparam name="P">
        /// The type of the packet to serialise.
        /// </typeparam>
        /// <param name="packet">
        /// The <see cref="Packet"/> object to serialise.
        /// </param>
        /// <returns>
        /// The RSA encrypted bytes that represent the given packet.
        /// </returns>
        public byte[] GetBytes<P>(P packet) where P : Packet
        {
            bool isRSACommunicationActive = IsRSACommunicationActive;
            byte[] unEncryptedData = PacketConverter.GetBytes(packet);

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
        public Packet GetPacket(Type packetType, byte[] serialisedPacket)
        {
            bool isRSACommunicationActive = serialisedPacket[0] == 1;
            byte[] data = new byte[serialisedPacket.Length - 1];
            Array.Copy(serialisedPacket, 1, data, 0, data.Length);

            return isRSACommunicationActive ? PacketConverter.GetPacket(packetType, DecryptBytes(data))
                : PacketConverter.GetPacket(packetType, data);
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
        public P GetPacket<P>(byte[] serialisedPacket) where P : Packet
        {
            bool isRSACommunicationActive = serialisedPacket[0] == 1;
            byte[] data = new byte[serialisedPacket.Length - 1];
            Array.Copy(serialisedPacket, 1, data, 0, data.Length);

            return isRSACommunicationActive ?
                PacketConverter.GetPacket<P>(DecryptBytes(data))
                : PacketConverter.GetPacket<P>(data);
        }

        #endregion Implementation of IPacketConverter

        /// <summary>
        /// Initialises the <see cref="RSAConnection"/> fully, and enables RSA functionality once it returns.
        /// </summary>
        private async void ExchangePublicKeys()
        {
            Connection.RegisterStaticPacketHandler<RSAKeyInformationRequest>((rsaKeyRequest, connection) =>
            {
                connection.UnRegisterStaticPacketHandler<RSAKeyInformationRequest>();

                CommunicationPartnerRSAPair = new RSAPair(rsaKeyRequest.PublicKey, rsaKeyRequest.KeySize);
                EncryptionProvider = new RSACryptoServiceProvider(CommunicationPartnerRSAPair.KeySize);
                Extensions.RSACryptoServiceProviderExtensions.ImportParametersFromXmlString(EncryptionProvider, CommunicationPartnerRSAPair.Public);

                connection.Send(new RSAKeyInformationResponse(RSAPair.Public, RSAPair.KeySize, rsaKeyRequest));
            });

            RSAKeyInformationResponse keyInformationResponse =
                await Connection.SendAsync<RSAKeyInformationResponse>(new RSAKeyInformationRequest(RSAPair.Public, RSAPair.KeySize)).ConfigureAwait(false);

            Connection.Logger.Log($"{Connection.GetType().Name} RSA Encryption active.", Enums.LogLevel.Information);
            IsRSACommunicationActive = true;
        }

        /// <summary>
        /// Decrypts the given bytes with the <see cref="DecryptionProvider"/>.
        /// </summary>
        /// <param name="bytes">The encrypted bytes to decrypt.</param>
        /// <returns>The decrypted, plaintext bytes.</returns>
        public byte[] DecryptBytes(byte[] bytes) 
        {
            List<byte[]> chunkData = new List<byte[]>();

            for(int currentIndex = 0; currentIndex < bytes.Length / MAX_DECRYPTION_BYTE_SIZE; currentIndex++)
                chunkData.Add(bytes.Skip(currentIndex * MAX_DECRYPTION_BYTE_SIZE).Take(MAX_DECRYPTION_BYTE_SIZE).ToArray());

            return chunkData.SelectMany(data => DecryptionProvider.Decrypt(data, XPOrHigher)).ToArray();
        }

        /// <summary>
        /// Encrypts the given bytes with the <see cref="EncryptionProvider"/>.
        /// </summary>
        /// <param name="bytes">The plaintext bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        public byte[] EncryptBytes(byte[] bytes)
        {
            List<byte[]> chunkData = new List<byte[]>();

            for(int currentIndex = 0; currentIndex <= bytes.Length / MAX_ENCRYPTION_BYTE_SIZE; currentIndex++)
                chunkData.Add(bytes.Skip(currentIndex * MAX_ENCRYPTION_BYTE_SIZE).Take(MAX_ENCRYPTION_BYTE_SIZE).ToArray());

            return chunkData.SelectMany(data => EncryptionProvider.Encrypt(data, XPOrHigher)).ToArray();
        }

        #endregion Methods
    }
}