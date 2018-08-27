#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 27.08.2018
//
// Last Modified By : Thomas
// Last Modified On : 27.08.2018
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
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace Network.RSA
{
    /// <summary>
    /// This class contains a udp connection to the given tcp client.
    /// It provides convenient methods to send and receive objects with a minimal serialization header.
    /// Compared to the <see cref="UdpConnection"/> the <see cref="SecureUdpConnection"/> does encrypt/decrypt sent/received bytes.
    /// </summary>
    public class SecureUdpConnection : UdpConnection, IPacketConverter
    {
        /// <summary>
        /// Encryption providers for encryption/decryption.
        /// </summary>
        private RSACryptoServiceProvider encryptionProvider;
        private RSACryptoServiceProvider decryptionProvider;

        /// <summary>
        /// The <see cref="SecureTcpConnection"/> is a <see cref="TcpConnection"/>.
        /// The only difference within the implementation:
        /// - A Packet will be converted with a different IPacketConverter.
        /// However, the packetConverter can still be set.
        /// </summary>
        private IPacketConverter externalPacketConverter;

        internal SecureUdpConnection(UdpClient udpClient, IPEndPoint remoteEndPoint, string publicKey, string privateKey, int keySize = 2048, bool writeLock = false)
            : base(udpClient, remoteEndPoint, writeLock, skipInitializationProcess:true)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
            KeySize = keySize;

            //Are we running on WinXP or higher?
            OperatingSystem operatingSystem = Environment.OSVersion;
            XPOrHigher = (operatingSystem.Platform == PlatformID.Win32NT) &&
                ((operatingSystem.Version.Major > 5) || ((operatingSystem.Version.Major == 5) &&
                (operatingSystem.Version.Minor >= 1)));

            encryptionProvider = new RSACryptoServiceProvider(KeySize);
            encryptionProvider.FromXmlString(publicKey);

            decryptionProvider = new RSACryptoServiceProvider(KeySize);
            decryptionProvider.FromXmlString(privateKey);

            externalPacketConverter = base.PacketConverter;
            base.PacketConverter = this;

            Init();
        }

        /// <summary>
        /// The PublicKey of this instance.
        /// </summary>
        public string PublicKey { get; private set; }

        /// <summary>
        /// The PrivateKey of this instance.
        /// </summary>
        public string PrivateKey { get; private set; }

        /// <summary>
        /// The used KeySize of this instance.
        /// </summary>
        public int KeySize { get; private set; }

        /// <summary>
        /// Is this application running on windowsXP or higher?
        /// </summary>
        public bool XPOrHigher { get; private set; }

        /// <summary>
        /// Use your own packetConverter to serialize/deserialze objects.
        /// Take care that the internal packet structure should still remain the same:
        ///     1. [16bits]  packet type
        ///     2. [32bits]  packet length
        ///     3. [xxbits]  packet data
        /// The default packetConverter uses reflection to get and set data within objects.
        /// Using your own packetConverter could result in a higher throughput.
        /// </summary>
        public override IPacketConverter PacketConverter
        {
            get => externalPacketConverter;
            set => externalPacketConverter = value;
        }

        /// <summary>
        /// Encrypts bytes with the <see cref="RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The Bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        private byte[] Encryption(byte[] bytes) => encryptionProvider.Encrypt(bytes, XPOrHigher);

        /// <summary>
        /// Decrypt bytes with the <see cref="RSACryptoServiceProvider" />
        /// </summary>
        /// <param name="bytes">The bytes to decrypt.</param>
        /// <returns>The decrypted bytes.</returns>
        private byte[] Decryption(byte[] bytes) => decryptionProvider.Decrypt(bytes, XPOrHigher);

        /// <summary>
        /// Gets the encrypted bytes of a <see cref="Packet"/>
        /// </summary>
        /// <param name="packet">The packet to encrypt.</param>
        /// <returns>The encrypted Packet.</returns>
        public byte[] GetBytes(Packet packet) => Encryption(PacketConverter.GetBytes(packet));

        /// <summary>
        /// Gets the encrypted packet of bytes.
        /// </summary>
        /// <param name="packetType">The packetType to encrypt.</param>
        /// <param name="data">The encrypted byte sequence.</param>
        /// <returns>A <see cref="Packet" /> object.</returns>
        public Packet GetPacket(Type packetType, byte[] data) => PacketConverter.GetPacket(packetType, Decryption(data));
    }
}
