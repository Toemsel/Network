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
using InTheHand.Net.Sockets;
using System;
using System.Linq;

namespace Network.Bluetooth
{
    /// <summary>
    /// Contains information about a bluetooth client.
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        internal DeviceInfo(BluetoothDeviceInfo deviceInfo)
        {
            DeviceName = deviceInfo.DeviceName;
            IsKnown = deviceInfo.Remembered;
            SignalStrength = deviceInfo.Rssi;
            LastSeen = deviceInfo.LastSeen;
            LastUsed = deviceInfo.LastUsed;
            BluetoothDeviceInfo = deviceInfo;
        }

        /// <summary>
        /// Generates the device infos.
        /// </summary>
        /// <param name="infos">The infos.</param>
        /// <returns>DeviceInfo[].</returns>
        internal static DeviceInfo[] GenerateDeviceInfos(BluetoothDeviceInfo[] infos)
        {
            return infos.ToList().Select(l => new DeviceInfo(l)).ToArray();
        }

        /// <summary>
        /// Gets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is known.
        /// </summary>
        /// <value><c>true</c> if this instance is known; otherwise, <c>false</c>.</value>
        public bool IsKnown { get; private set; }

        /// <summary>
        /// Gets the signal strength.
        /// </summary>
        /// <value>The signal strength.</value>
        public int SignalStrength { get; private set; }

        /// <summary>
        /// Gets the last seen.
        /// </summary>
        /// <value>The last seen.</value>
        public DateTime LastSeen { get; private set; }

        /// <summary>
        /// Gets the last used.
        /// </summary>
        /// <value>The last used.</value>
        public DateTime LastUsed { get; private set; }

        /// <summary>
        /// Gets the bluetooth device information.
        /// </summary>
        /// <value>The bluetooth device information.</value>
        internal BluetoothDeviceInfo BluetoothDeviceInfo { get; private set; }
    }
}
#endif