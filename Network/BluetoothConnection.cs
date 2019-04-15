#if NET46

using InTheHand.Net.Sockets;
using Network.Bluetooth;
using Network.Enums;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// Builds upon the <see cref="Connection"/> class, implementing Bluetooth and allowing for messages to be conveniently
    /// sent without a large serialisation header.
    /// </summary>
    /// <remarks>
    /// This class is only available for .NET Framework 4.6 and above. This class is not compiled for .NET Standard, as a
    /// key dependency is only available for the .NET Framework (looking at you, InTheHand).
    /// </remarks>
    public class BluetoothConnection : Connection
    {
        #region Variables

        /// <summary>
        /// The <see cref="Stream"/> for reading and writing data.
        /// </summary>
        private NetworkStream stream;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnection"/> class.
        /// </summary>
        /// <param name="deviceInfo">The device Bluetooth information.</param>
        internal BluetoothConnection(DeviceInfo deviceInfo) : this()
        {
            DeviceInfo = deviceInfo.BluetoothDeviceInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnection"/> class.
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

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The device info of the connected device.
        /// </summary>
        private BluetoothDeviceInfo DeviceInfo { get; set; }

        /// <summary>
        /// The bluetooth client that sends and receives data.
        /// </summary>
        private BluetoothClient Client { get; set; }

        /// <summary>
        /// The signal strength of the paired device.
        /// </summary>
        public int SignalStrength { get { return DeviceInfo.Rssi; } }

        /// <summary>
        /// Whether Bluetooth is supported by the current device.
        /// </summary>
        public static bool IsBluetoothSupported
        {
            get
            {
                try
                {
                    new BluetoothClient();
                }
                catch (PlatformNotSupportedException)
                {
                    return false;
                }
                return true;
            }
        }

        /// <inheritdoc />
        public override IPEndPoint IPLocalEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <inheritdoc />
        public override IPEndPoint IPRemoteEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.None, 0);
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        #endregion Properties

        #region Methods

        /// <summary>
        /// Attempts to connect to the remote endpoint asynchronously.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation, with the promise of a <see cref="ConnectionResult"/> on completion.
        /// </returns>
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

        /// <inheritdoc />
        protected override void CloseHandler(CloseReason closeReason)
        {
            Close(closeReason, true);
        }

        /// <inheritdoc />
        protected override void CloseSocket() => Client.Close();

        /// <inheritdoc />
        protected override void HandleUnknownPacket() => Close(CloseReason.UnknownPacket, true);

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void WriteBytes(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            if (ForceFlush) stream.Flush();
        }

        #endregion Methods
    }
}

#endif