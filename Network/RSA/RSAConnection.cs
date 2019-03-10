#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 29.09.2018
//
// Last Modified By : Thomas
// Last Modified On : 29.09.2018
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2018
// </copyright>
// <License>
// GNU LESSER GENERAL PUBLIC LICENSE
// </License>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ***********************************************************************
#endregion Licence - LGPLv3
using Network.Converter;
using Network.Interfaces;
using Network.Packets.RSA;
using System;
using System.Security.Cryptography;

namespace Network.RSA
{
    /// <summary>
    /// A RSAConnection contains all required information to handle RSA encrypted data.
    /// </summary>
    /// <seealso cref="Network.Interfaces.IRSACapability" />
    /// <seealso cref="Network.Interfaces.IRSAConnection" />
    internal class RSAConnection : IPacketConverter, IRSACapability, IRSAConnection
    {
        private volatile RSAPair communicationPartnerRSAPair;
        private volatile RSACryptoServiceProvider encryptionProvider;
        private volatile bool isRSACommunicationActive = false;

        public RSAConnection(Connection connection, RSAPair rsaPair)
        {
            Connection = connection;
            RSAPair = rsaPair;

            DecryptionProvider = new RSACryptoServiceProvider(RSAPair.KeySize);
            Extensions.RSACryptoServiceProviderExtensions.FromXmlString(DecryptionProvider, RSAPair.Private);

            //Are we running on WinXP or higher?
            OperatingSystem operatingSystem = Environment.OSVersion;
            XPOrHigher = (operatingSystem.Platform == PlatformID.Win32NT) &&
                ((operatingSystem.Version.Major > 5) || ((operatingSystem.Version.Major == 5) &&
                (operatingSystem.Version.Minor >= 1)));

            //Setup RSA related packets.
            ExchangePublicKeys();
        }

        private Connection Connection { get; set; }

        /// <summary>
        /// Use your own packetConverter to serialize/deserialze objects.
        /// Take care that the internal packet structure should still remain the same:
        ///     1. [16bits]  packet type
        ///     2. [32bits]  packet length
        ///     3. [xxbits]  packet data
        /// The default packetConverter uses reflection to get and set data within objects.
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
            get => communicationPartnerRSAPair;
            set => communicationPartnerRSAPair = value;
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

        /// <summary>
        /// Initializes the RSA communication data.
        /// Sends our information to the communication partner.
        /// Subscribes to the RSA packet events.
        /// </summary>
        private async void ExchangePublicKeys()
        {
            Connection.RegisterStaticPacketHandler<RSAKeyInformationRequest>((rsaKeyRequest, connection) =>
            {
                connection.UnRegisterStaticPacketHandler<RSAKeyInformationRequest>();

                CommunicationPartnerRSAPair = new RSAPair(rsaKeyRequest.PublicKey, rsaKeyRequest.KeySize);
                EncryptionProvider = new RSACryptoServiceProvider(CommunicationPartnerRSAPair.KeySize);
                Extensions.RSACryptoServiceProviderExtensions.FromXmlString(EncryptionProvider, CommunicationPartnerRSAPair.Public);

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
        public byte[] Decrypt(byte[] bytes) => DecryptionProvider.Decrypt(bytes, XPOrHigher);

        /// <summary>
        /// Encrypts bytes with the <see cref="T:System.Security.Cryptography.RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The Bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        public byte[] Encrypt(byte[] bytes) => EncryptionProvider.Encrypt(bytes, XPOrHigher);

        /// <summary>
        /// Gets the encrypted bytes of a <see cref="Packet"/>
        /// </summary>
        /// <param name="packet">The packet to encrypt.</param>
        /// <returns>The encrypted Packet.</returns>
        public byte[] GetBytes(Packet packet)
        {
            bool isRSACommunicationActive = IsRSACommunicationActive;
            byte[] unEncryptedData = PacketConverter.GetBytes(packet);

            byte[] rsaStatus = BitConverter.GetBytes(isRSACommunicationActive);
            byte[] packetData = isRSACommunicationActive ? Encrypt(unEncryptedData) : unEncryptedData;
            byte[] packetDataWithStatus = new byte[packetData.Length + 1];
            Array.Copy(rsaStatus, 0, packetDataWithStatus, 0, 1);
            Array.Copy(packetData, 0, packetDataWithStatus, 1, packetData.Length);
            return packetDataWithStatus;
        }

        /// <summary>
        /// Gets the encrypted packet of bytes.
        /// </summary>
        /// <param name="packetType">The packetType to encrypt.</param>
        /// <param name="data">The encrypted byte sequence.</param>
        /// <returns>A <see cref="Packet" /> object.</returns>
        public Packet GetPacket(Type packetType, byte[] rawData)
        {
            bool isRSACommunicationActive = rawData[0] == 1;
            byte[] data = new byte[rawData.Length - 1];
            Array.Copy(rawData, 1, data, 0, data.Length);

            return isRSACommunicationActive ? PacketConverter.GetPacket(packetType, Decrypt(data))
                : PacketConverter.GetPacket(packetType, data);
        }
    }
}
