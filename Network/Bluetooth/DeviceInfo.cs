#if NET46

using InTheHand.Net.Sockets;
using System;
using System.Linq;

namespace Network.Bluetooth
{
    /// <summary>
    /// Stores information about a Bluetooth device.
    /// </summary>
    /// <remarks>
    /// This class is only applicable if the build is for the .NET Framework 4.6. It is only compiled if the 'NET46'
    /// preprocessor variable is set.
    /// </remarks>
    public class DeviceInfo
    {
        #region Constructors

        /// <summary>
        /// Constructs and returns a new instance of the <see cref="DeviceInfo"/>, mapping the given
        /// <see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/> to the <see cref="DeviceInfo"/>.
        /// </summary>
        /// <param name="deviceInfo">
        /// The <see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/> whose properties to use for the new
        /// <see cref="DeviceInfo"/> instance.
        /// </param>
        internal DeviceInfo(BluetoothDeviceInfo deviceInfo)
        {
            DeviceName = deviceInfo.DeviceName;
            IsKnown = deviceInfo.Remembered;
            SignalStrength = deviceInfo.Rssi;
            LastSeen = deviceInfo.LastSeen;
            LastUsed = deviceInfo.LastUsed;
            BluetoothDeviceInfo = deviceInfo;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The name of the Bluetooth device.
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// Whether the device is already known.
        /// </summary>
        public bool IsKnown { get; }

        /// <summary>
        /// The signal strength of the Bluetooth connection.
        /// </summary>
        public int SignalStrength { get; }

        /// <summary>
        /// The last time that the device was seen via Bluetooth.
        /// </summary>
        public DateTime LastSeen { get; }

        /// <summary>
        /// The last time that the device was used.
        /// </summary>
        public DateTime LastUsed { get; }

        /// <summary>
        /// The <see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/> for the device.
        /// </summary>
        internal BluetoothDeviceInfo BluetoothDeviceInfo { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// For each of the given <see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/>s, generates a
        /// <see cref="DeviceInfo"/> and returns the generated array.
        /// </summary>
        /// <param name="infos">
        /// An array of <see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/>s, for each of which to generate the
        /// corresponding <see cref="DeviceInfo"/>.
        /// </param>
        /// <returns>
        /// An array of <see cref="DeviceInfo"/>s, one for each of the given<see cref="InTheHand.Net.Sockets.BluetoothDeviceInfo"/>s.
        /// </returns>
        internal static DeviceInfo[] GenerateDeviceInfos(BluetoothDeviceInfo[] infos)
        {
            return infos.ToList().Select(info => new DeviceInfo(info)).ToArray();
        }

        #endregion Methods
    }
}

#endif