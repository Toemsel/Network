#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-24-2015
//
// Last Modified By : Thomas
// Last Modified On : 27-08-2018
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
#if NET46
using InTheHand.Net.Sockets;
using Network.Bluetooth;
#endif
using Network.RSA;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// The possible results of a connection attempt.
    /// </summary>
    public enum ConnectionResult
    {
        /// <summary>
        /// A connection could be established
        /// </summary>
        Connected,
        /// <summary>
        /// A connection couldn't be established.
        /// IP + Port correct? Firewall rules?
        /// </summary>
        Timeout,
        /// <summary>
        /// Could not establish a UDP connection.
        /// The depending TCP connection is not alive.
        /// </summary>
        TCPConnectionNotAlive
    }

    /// <summary>
    /// This factory creates instances of Tcp and Udp connections.
    /// </summary>
    public static class ConnectionFactory
    {
        /// <summary>
        /// The timeout of a connection attempt in [ms]
        /// </summary>
        public const int CONNECTION_TIMEOUT = 8000;

#if NET46
        /// <summary>
        /// The GUID of this assembly, needed for bluetooth connections.
        /// </summary>
        internal static Guid GUID;

        /// <summary>
        /// Set the GUID of this assembly.
        /// </summary>
        static ConnectionFactory()
        {
            GUID = Assembly.GetAssembly(typeof(Connection)).GetType().GUID;
        }

        /// <summary>
        /// Gets all the bluetooth devices in range.
        /// </summary>
        /// <returns>The bluetooth devices in range.</returns>
        public static DeviceInfo[] GetBluetoothDevices() { return DeviceInfo.GenerateDeviceInfos(new BluetoothClient().DiscoverDevicesInRange()); }

        /// <summary>
        /// Gets all the bluetooth devices in range async.
        /// </summary>
        /// <returns></returns>
        public async static Task<DeviceInfo[]> GetBluetoothDevicesAsync()
        {
            return await Task.Factory.StartNew(() => DeviceInfo.GenerateDeviceInfos(new BluetoothClient().DiscoverDevices()));
        }

        /// <summary>
        /// Creates a new instance of the BluetoothConnection with the given device info.
        /// </summary>
        /// <param name="bluetoothDeviceInfo">The device to pair with.</param>
        /// <returns>The connection to send and receive data.</returns>
        public static Tuple<ConnectionResult, BluetoothConnection> CreateBluetoothConnection(DeviceInfo bluetoothDeviceInfo)
        {
            BluetoothConnection bluetoothConnection = new BluetoothConnection(bluetoothDeviceInfo);
            var result = bluetoothConnection.TryConnect().Result;
            return new Tuple<ConnectionResult, BluetoothConnection>(result, bluetoothConnection);
        }

        /// <summary>
        /// Creates a new instance of the BluetoothConnection with the given device info.
        /// </summary>
        /// <param name="bluetoothDeviceInfo">The device to pair with.</param>
        /// <returns>The connection to send and receive data.</returns>
        public async static Task<Tuple<ConnectionResult, BluetoothConnection>> CreateBluetoothConnectionAsync(DeviceInfo bluetoothDeviceInfo)
        {
            BluetoothConnection bluetoothConnection = new BluetoothConnection(bluetoothDeviceInfo);
            var result = await bluetoothConnection.TryConnect();
            return new Tuple<ConnectionResult, BluetoothConnection>(result, bluetoothConnection);
        }

        /// <summary>
        /// Creates a new instance of the BluetoothConnection with the given client.
        /// </summary>
        /// <param name="bluetoothClient">The client to create a connection with.</param>
        /// <returns>The result.</returns>
        internal static BluetoothConnection CreateBluetoothConnection(BluetoothClient bluetoothClient)
        {
            return new BluetoothConnection(bluetoothClient);
        }
#endif

        /// <summary>
        /// Creates a new tcp connection and tries to connect to the given endpoint.
        /// </summary>
        /// <param name="ipAddress">The ip address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>A tcp connection object if the successfully connected. Else null.</returns>
        public static TcpConnection CreateTcpConnection(string ipAddress, int port, out ConnectionResult connectionResult)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateTcpConnectionAsync(ipAddress, port).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Creates a new tcp secure connection and tries to connect to the given endpoint.
        /// </summary>
        /// <param name="ipAddress">The ip address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>A tcp connection object if the successfully connected. Else null.</returns>
        public static TcpConnection CreateSecureTcpConnection(string ipAddress, int port, string publicKey, string privateKey, out ConnectionResult connectionResult, int keySize = 2048)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateSecureTcpConnectionAsync(ipAddress, port, publicKey, privateKey, keySize).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Creates a new tcp connection and tries to connect to the given endpoint async.
        /// </summary>
        /// <param name="ipAddress">The ip address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>A tcp connection object if the successfully connected. Else null.</returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnectionAsync(string ipAddress, int port)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
                Task connectTask = Task.Factory.StartNew(() => tcpClient.Connect(ipAddress, port));
                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                        return new Tuple<TcpConnection, ConnectionResult>(new TcpConnection(tcpClient), ConnectionResult.Connected);
            }
            catch { }

            return new Tuple<TcpConnection, ConnectionResult>(null, ConnectionResult.Timeout);
        }

        /// <summary>
        /// Creates a new secure tcp connection and tries to connect to the given endpoint async.
        /// </summary>
        /// <param name="ipAddress">The ip address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>A tcp connection object if the successfully connected. Else null.</returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateSecureTcpConnectionAsync(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
                Task connectTask = Task.Factory.StartNew(() => tcpClient.Connect(ipAddress, port));
                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                    return new Tuple<TcpConnection, ConnectionResult>(new SecureTcpConnection(publicKey, privateKey, tcpClient, keySize), ConnectionResult.Connected);
            }
            catch { }

            return new Tuple<TcpConnection, ConnectionResult>(null, ConnectionResult.Timeout);
        }


        /// <summary>
        /// Wraps the given tcpClient into the networks tcp connection.
        /// </summary>
        /// <param name="tcpClient">The connected tcp client.</param>
        /// <returns>The TcpConnection.</returns>
        /// <exception cref="System.ArgumentException">Socket is not connected.</exception>
        public static TcpConnection CreateTcpConnection(TcpClient tcpClient)
        {
            if (!tcpClient.Connected) throw new ArgumentException("Socket is not connected.");
            return new TcpConnection(tcpClient);
        }

        /// <summary>
        /// Wraps the given tcpClient into the networks tcp connection.
        /// </summary>
        /// <param name="tcpClient">The connected tcp client.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>The TcpConnection.</returns>
        /// <exception cref="System.ArgumentException">Socket is not connected.</exception>
        public static TcpConnection CreateSecureTcpConnection(TcpClient tcpClient, string publicKey, string privateKey, int keySize = 2048)
        {
            if (!tcpClient.Connected) throw new ArgumentException("Socket is not connected.");
            return new SecureTcpConnection(publicKey, privateKey, tcpClient, keySize);
        }

        /// <summary>
        /// Creates a new instance of a udp connection.
        /// </summary>
        /// <param name="tcpConnection">The tcp connection to establish the udp connection.</param>
        /// <returns>The UdpConnection.</returns>
        public static UdpConnection CreateUdpConnection(TcpConnection tcpConnection, out ConnectionResult connectionResult)
        {
            Tuple<UdpConnection, ConnectionResult> connectionRequest = CreateUdpConnectionAsync(tcpConnection).Result;
            connectionResult = connectionRequest.Item2;
            return connectionRequest.Item1;
        }

        /// <summary>
        /// Creates a new instance of a secure udp connection.
        /// </summary>
        /// <param name="tcpConnection">The tcp connection to establish the udp connection.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>The UdpConnection.</returns>
        public static UdpConnection CreateSecureUdpConnection(TcpConnection tcpConnection, string publicKey, string privateKey, out ConnectionResult connectionResult, int keySize = 2048)
        {
            Tuple<UdpConnection, ConnectionResult> connectionRequest = CreateSecureUdpConnectionAsync(tcpConnection, publicKey, privateKey, keySize).Result;
            connectionResult = connectionRequest.Item2;
            return connectionRequest.Item1;
        }

        /// <summary>
        /// Creates a new instance of a udp connection async.
        /// </summary>
        /// <param name="tcpConnection">The tcp connection to establish the udp connection.</param>
        /// <returns>Task&lt;UdpConnection&gt;.</returns>
        /// <exception cref="ArgumentException">TcpConnection is not connected to the endpoint.</exception>
        public static async Task<Tuple<UdpConnection, ConnectionResult>> CreateUdpConnectionAsync(TcpConnection tcpConnection)
        {
            UdpConnection udpConnection = null;
            ConnectionResult connectionResult = ConnectionResult.Connected;
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            cancellationToken.CancelAfter(CONNECTION_TIMEOUT);
            if (tcpConnection == null || !tcpConnection.IsAlive)
                return new Tuple<UdpConnection, ConnectionResult>(udpConnection, ConnectionResult.TCPConnectionNotAlive);
            tcpConnection.EstablishUdpConnection((localEndPoint, RemoteEndPoint) => udpConnection = new UdpConnection(new UdpClient(localEndPoint), RemoteEndPoint));
            while (udpConnection == null && !cancellationToken.IsCancellationRequested) await Task.Delay(25);
            if (udpConnection == null && cancellationToken.IsCancellationRequested) connectionResult = ConnectionResult.Timeout;
            return new Tuple<UdpConnection, ConnectionResult>(udpConnection, connectionResult);
        }

        /// <summary>
        /// Creates a new instance of a udp connection async.
        /// </summary>
        /// <param name="tcpConnection">The tcp connection to establish the udp connection.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>Task&lt;UdpConnection&gt;.</returns>
        /// <exception cref="ArgumentException">TcpConnection is not connected to the endpoint.</exception>
        public static async Task<Tuple<UdpConnection, ConnectionResult>> CreateSecureUdpConnectionAsync(TcpConnection tcpConnection, string publicKey, string privateKey, int keySize = 2048)
        {
            UdpConnection udpConnection = null;
            ConnectionResult connectionResult = ConnectionResult.Connected;
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            cancellationToken.CancelAfter(CONNECTION_TIMEOUT);
            if (tcpConnection == null || !tcpConnection.IsAlive)
                return new Tuple<UdpConnection, ConnectionResult>(udpConnection, ConnectionResult.TCPConnectionNotAlive);
            tcpConnection.EstablishUdpConnection((localEndPoint, RemoteEndPoint) => udpConnection = new SecureUdpConnection(new UdpClient(localEndPoint), RemoteEndPoint, publicKey, privateKey, keySize));
            while (udpConnection == null && !cancellationToken.IsCancellationRequested) await Task.Delay(25);
            if (udpConnection == null && cancellationToken.IsCancellationRequested) connectionResult = ConnectionResult.Timeout;
            return new Tuple<UdpConnection, ConnectionResult>(udpConnection, connectionResult);
        }

        /// <summary>
        /// Creates a new instance of a connection container.
        /// </summary>
        /// <returns>ConnectionContainer.</returns>
        public static ClientConnectionContainer CreateClientConnectionContainer(string ipAddress, int port)
        {
            var clientConnectionContainer = new ClientConnectionContainer(ipAddress, port);
            clientConnectionContainer.Initialize();
            return clientConnectionContainer;
        }

        /// <summary>
        /// Creates a new instance of a secure-connection container. (RSA Encryption)
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// </summary>
        /// <returns>ConnectionContainer.</returns>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048)
        {
            var secureClientConnectionContainer = new SecureClientConnectionContainer(ipAddress, port, publicKey, privateKey, keySize);
            secureClientConnectionContainer.Initialize();
            return secureClientConnectionContainer;
        }

        /// <summary>
        /// Creates a new instance of a connection container.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <param name="udpConnection">The UDP connection.</param>
        /// <returns>ConnectionContainer.</returns>
        /// <exception cref="System.ArgumentException">TCP and UDP connection must be connected to an endpoint.</exception>
        public static ClientConnectionContainer CreateClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection)
        {
            if (tcpConnection == null ||!tcpConnection.IsAlive)
                throw new ArgumentException("TCP connection must be connected to an endpoint.");

            var clientConnectionContainer = new ClientConnectionContainer(tcpConnection, udpConnection);
            clientConnectionContainer.Initialize();
            return clientConnectionContainer;
        }

        /// <summary>
        /// Creates a new instance of a secure-connection container. (RSA Encryption)
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <param name="udpConnection">The UDP connection.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>ConnectionContainer.</returns>
        /// <exception cref="System.ArgumentException">TCP and UDP connection must be connected to an endpoint.</exception>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection, string publicKey, string privateKey, int keySize = 2048)
        {
            if (tcpConnection == null || !tcpConnection.IsAlive)
                throw new ArgumentException("TCP connection must be connected to an endpoint.");
            
            var secureClientConnectionContainer = new SecureClientConnectionContainer(tcpConnection, udpConnection, publicKey, privateKey, keySize);
            secureClientConnectionContainer.Initialize();
            return secureClientConnectionContainer;
        }

        /// <summary>
        /// Creates the server connection container.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        /// <returns>ServerConnectionContainer.</returns>
        public static ServerConnectionContainer CreateServerConnectionContainer(int port, bool start = true) => new ServerConnectionContainer(port, start);

        /// <summary>
        /// Creates a secure server connection container.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <returns>ServerConnectionContainer.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(int port, string publicKey, string privateKey, int keySize = 2048, bool start = true) => new SecureServerConnectionContainer(port, publicKey, privateKey, keySize, start);

        /// <summary>
        /// Creates the server connection container.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        /// <returns>ServerConnectionContainer.</returns>
        public static ServerConnectionContainer CreateServerConnectionContainer(string ipAddress, int port, bool start = true) => new ServerConnectionContainer(ipAddress, port, start);

        /// <summary>
        /// Creates a secure server connection container.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="publicKey">The public key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The keySize.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        /// <returns>ServerConnectionContainer.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048, bool start = true) => new SecureServerConnectionContainer(ipAddress, port, publicKey, privateKey, keySize, start);
    }
}