#if NET46
#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 05-28-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 10-10-2015
// ***********************************************************************
// <copyright>
// Company: Indie-Dev
// Thomas Christof (c) 2015
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
using System;
using System.Net;
using Network.Enums;
using InTheHand.Net.Sockets;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Network.Bluetooth;

namespace Network
{
    public class BluetoothConnection : Connection
    {
        private NetworkStream stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnection"/> class.
        /// The client would like to establish a connection.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        internal BluetoothConnection(DeviceInfo deviceInfo) : this()
        {
            DeviceInfo = deviceInfo.BluetoothDeviceInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnection"/> class.
        /// The server received a request.
        /// </summary>
        /// <param name="bluetoothClient">The bluetooth client.</param>
        internal BluetoothConnection(BluetoothClient bluetoothClient) : this()
        {
            Client = bluetoothClient;
            stream = Client.GetStream();
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnection"/> class.
        /// </summary>
        internal BluetoothConnection()
        {
            KeepAlive = true;
        }

        /// <summary>
        /// Tries to connect to the endpoint.
        /// </summary>
        /// <returns>Task&lt;ConnectionResult&gt;.</returns>
        internal async Task<ConnectionResult> TryConnect()
        {
            Client = new BluetoothClient();

            try
            {
                await Task.Factory.FromAsync(Client.BeginConnect(DeviceInfo.DeviceAddress, ConnectionFactory.GUID, (f) => { }, null), Client.EndConnect);
                stream = Client.GetStream();
            }
            catch
            {
                return ConnectionResult.Timeout;
            }

            Init();
            return ConnectionResult.Connected;
        }

        /// <summary>
        /// The device info of the connected device.
        /// </summary>
        private BluetoothDeviceInfo DeviceInfo { get; set; }

        /// <summary>
        /// The bluetooth client connected to.
        /// </summary>
        private BluetoothClient Client { get; set; }

        /// <summary>
        /// Gets the signal strength of the paired device.
        /// </summary>
        public int SignalStrength { get { return DeviceInfo.Rssi; } }

        /// <summary>
        /// Gets if Bluetooth is supported by the current device.
        /// [True] if Bluetooth is supported. [False] if not.
        /// </summary>
        public static bool IsBluetoothSupported
        {
            get
            {
                try
                {
                    new BluetoothClient();
                }
                catch(PlatformNotSupportedException)
                {
                    return false;
                }
                return true;
            }
        }

        public override bool DualMode
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override bool Fragment
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override int HopLimit
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override IPEndPoint IPLocalEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        public override IPEndPoint IPRemoteEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        public override bool IsRoutingEnabled
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override bool NoDelay
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override short TTL
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override bool UseLoopback
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        protected override void CloseHandler(CloseReason closeReason)
        {
            Close(closeReason, true);
        }

        protected override void CloseSocket()
        {
            Client.Close();
        }

        protected override void HandleUnknownPacket()
        {
            Close(CloseReason.UnknownPacket, true);
        }

        protected override byte[] ReadBytes(int amount)
        {
            if (amount == 0) return new byte[0];
            byte[] requestedBytes = new byte[amount];
            int receivedIndex = 0;
            while (receivedIndex < amount)
            {
                while (Client.Available == 0)
                    Thread.Sleep(IntPerformance);

                int readAmount = (amount - receivedIndex >= Client.Available) ? Client.Available : amount - receivedIndex;
                stream.Read(requestedBytes, receivedIndex, readAmount);
                receivedIndex += readAmount;
            }

            return requestedBytes;
        }

        protected override void WriteBytes(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            if (ForceFlush) stream.Flush();
        }
    }
}
#endif