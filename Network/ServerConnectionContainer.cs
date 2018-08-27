#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas Christof
// Created          : 02-03-2016
//
// Last Modified By : Thomas Christof
// Last Modified On : 28.05.2016
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Network.Enums;
using InTheHand.Net.Sockets;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// Is able to open and close connections to clients.
    /// Handles basic client connection requests and provides useful methods
    /// to manage the existing connection.
    /// </summary>
    public class ServerConnectionContainer : ConnectionContainer
    {
        private TcpListener tcpListener;
        private BluetoothListener bluetoothListener;
        private event Action<Connection, ConnectionType> connectionEstablished;
        private event Action<Connection, ConnectionType, CloseReason> connectionLost;
        private ConcurrentDictionary<TcpConnection, List<UdpConnection>> connections = new ConcurrentDictionary<TcpConnection, List<UdpConnection>>();
        private ConcurrentBag<BluetoothConnection> bluetoothConnections = new ConcurrentBag<BluetoothConnection>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to tcp/udp/bluetooth clients.</param>
        internal ServerConnectionContainer(string ipAddress, int port, bool start = true)
            : base(ipAddress, port)
        {
            if (start)
                Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnectionContainer" /> class.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="start">if set to <c>true</c> then the instance automatically starts to listen to clients.</param>
        internal ServerConnectionContainer(int port, bool start = true)
            : this(System.Net.IPAddress.Any.ToString(), port, start)
        {

        }

        /// <summary>
        /// Gets the <see cref="List{UdpConnection}"/> with the specified TCP connection.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <returns>List&lt;UdpConnection&gt;.</returns>
        public List<UdpConnection> this[TcpConnection tcpConnection]
        {
            get
            {
                if(connections.ContainsKey(tcpConnection))
                    return connections[tcpConnection];
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="TcpConnection"/> with the specified UDP connection.
        /// </summary>
        /// <param name="udpConnection">The UDP connection.</param>
        /// <returns>TcpConnection.</returns>
        public TcpConnection this[UdpConnection udpConnection]
        {
            get { return connections.SingleOrDefault(c => c.Value.Count(uc => uc.GetHashCode().Equals(udpConnection.GetHashCode())) > 0).Key; }
        }

        /// <summary>
        /// Gets a value indicating whether the tcp server is online or not.
        /// </summary>
        /// <value><c>true</c> if this instance is online; otherwise, <c>false</c>.</value>
        public bool IsTCPOnline { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether the bluetooth server is online or not.
        /// </summary>
        public bool IsBluetoothOnline { get; private set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether [allow UDP connections].
        /// </summary>
        /// <value><c>true</c> if [allow UDP connections]; otherwise, <c>false</c>.</value>
        public bool AllowUDPConnections { get; set; } = true;

        /// <summary>
        /// Gets or sets how many UDP connection are accepted. If the client requests another
        /// udp connection which exceeds this limit, the TCP connection and all the UDP connections will be closed.
        /// </summary>
        public int UDPConnectionLimit { get; set; } = 1;

        /// <summary>
        /// Gets or sets if the server is listening to bluetooth connections.
        /// Existing bluetooth connections wont be closed if you toggle this property.
        /// The server wont start or stop if you toggle this value.
        /// </summary>
        public bool AllowBluetoothConnections { get; set; } = false;

        /// <summary>
        /// The maximum amount of pending bluetooth connections.
        /// </summary>
        public int MaxBluetoothPendingQueue { get; set; } = 15;

        /// <summary>
        /// Gets all the connected TCP connections.
        /// </summary>
        /// <value>The tc p_ connections.</value>
        public List<TcpConnection> TCP_Connections { get { return connections.Keys.ToList(); } }

        /// <summary>
        /// Gets all the connected UDP connections.
        /// </summary>
        /// <value>The ud p_ connections.</value>
        public List<UdpConnection> UDP_Connections { get { return connections.Values.SelectMany(c => c).ToList(); } }

        /// <summary>
        /// Gets all the connected BLUETOOTH connections.
        /// </summary>
        public List<BluetoothConnection> BLUETOOTH_Connections { get { return bluetoothConnections.ToList(); } }

        /// <summary>
        /// Gets the connection count. (Clients)
        /// </summary>
        public int Count { get { return connections.Count + bluetoothConnections.Count; } }

        /// <summary>
        /// Occurs when [connection closed]. This action will be called if a TCP or an UDP has been closed.
        /// If a TCP connection has been closed, all its attached UDP connections are lost as well.
        /// If a UDP connection has been closed, the attached TCP connection may still be alive.
        /// </summary>
        public event Action<Connection, ConnectionType, CloseReason> ConnectionLost
        {
            add { connectionLost += value; }
            remove { connectionLost -= value; }
        }

        /// <summary>
        /// Occurs when a TCP or an UDP connection has been established.
        /// </summary>
        public event Action<Connection, ConnectionType> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

        /// <summary>
        /// Starts to listen to tcp and bluetooth clients.
        /// </summary>
        public void Start()
        {
            StartTCPListener();
            StartBluetoothListener();
        }

        /// <summary>
        /// Starts to listen to the given port and ipAddress.
        /// </summary>
        public async void StartTCPListener()
        {
            if (IsTCPOnline) return;

            tcpListener = new TcpListener(System.Net.IPAddress.Parse(IPAddress), Port);
            IsTCPOnline = !IsTCPOnline;
            tcpListener.Start();

            while (IsTCPOnline)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                TcpConnection tcpConnection = CreateTcpConnection(tcpClient);
                tcpConnection.ConnectionClosed += connectionClosed;
                tcpConnection.ConnectionEstablished += udpConnectionReceived;
                connections.GetOrAdd(tcpConnection, new List<UdpConnection>());

                //Inform all subscribers.
                if (connectionEstablished != null &&
                    connectionEstablished.GetInvocationList().Length > 0)
                    connectionEstablished(tcpConnection, ConnectionType.TCP);

                KnownTypes.ForEach(tcpConnection.AddExternalPackets);
                //Now that the server registered all the methods, unlock the client.
                tcpConnection.UnlockRemoteConnection();
            }
        }

        /// <summary>
        /// Starts to listen to available bluetooth connections.
        /// </summary>
        public async void StartBluetoothListener()
        {
            if (IsBluetoothOnline || !AllowBluetoothConnections || !BluetoothConnection.IsBluetoothSupported) return;

            bluetoothListener = new BluetoothListener(ConnectionFactory.GUID);
            bluetoothListener.Start();
            IsBluetoothOnline = !IsBluetoothOnline;

            while (IsBluetoothOnline)
            {
                BluetoothClient bluetoothClient = await Task.Factory.FromAsync(bluetoothListener.BeginAcceptBluetoothClient, bluetoothListener.EndAcceptBluetoothClient, TaskCreationOptions.PreferFairness);
                BluetoothConnection bluetoothConnection = ConnectionFactory.CreateBluetoothConnection(bluetoothClient);
                bluetoothConnection.ConnectionClosed += connectionClosed;

                //Inform all subscribers.
                if (connectionEstablished != null &&
                    connectionEstablished.GetInvocationList().Length > 0)
                    connectionEstablished(bluetoothConnection, ConnectionType.Bluetooth);

                //Unlock the remote bluetooth connection.
                bluetoothConnection.UnlockRemoteConnection();
            }
        }

        /// <summary>
        /// A UDP connection has been established.
        /// </summary>
        /// <param name="arg1">The arg1.</param>
        /// <param name="arg2">The arg2.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void udpConnectionReceived(TcpConnection tcpConnection, UdpConnection udpConnection)
        {
            if (!AllowUDPConnections || this[tcpConnection].Count >= UDPConnectionLimit)
            {
                CloseReason closeReason = (this[tcpConnection].Count >= UDPConnectionLimit) ? CloseReason.UdpLimitExceeded : CloseReason.InvalidUdpRequest;
                tcpConnection.Close(closeReason, true);
                return;
            }

            this[tcpConnection].Add(udpConnection);
            udpConnection.ConnectionClosed += connectionClosed;
            KnownTypes.ForEach(udpConnection.AddExternalPackets);

            //Inform all subscribers.
            if (connectionEstablished != null &&
                connectionEstablished.GetInvocationList().Length > 0)
                connectionEstablished(udpConnection, ConnectionType.UDP);
        }

        /// <summary>
        /// TCPs the or UDP connection closed.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        /// <param name="connection">The connection.</param>
        private void connectionClosed(CloseReason closeReason, Connection connection)
        {
            if(connection.GetType().Equals(typeof(TcpConnection)))
            {
                List<UdpConnection> udpConnections = new List<UdpConnection>();
                TcpConnection tcpConnection = (TcpConnection)connection;
                while (!connections.TryRemove(tcpConnection, out udpConnections))
                    Thread.Sleep(new Random().Next(0, 8)); //If we could not remove the tcpConnection, try it again.
                udpConnections.ForEach(u => u.ExternalClose(closeReason));
            }
            else if(connection.GetType().Equals(typeof(UdpConnection)))
            {
                TcpConnection tcpConnection = this[(UdpConnection)connection];
                if (tcpConnection == null) return; //UDP connection already removed
                //because the TCP connection is already dead.
                connections[tcpConnection].Remove((UdpConnection)connection);
            }
            else if (connection.GetType().Equals(typeof(BluetoothConnection)))
            {
                //Remove the bluetooth connection from the bag.
                bluetoothConnections = new ConcurrentBag<BluetoothConnection>(bluetoothConnections.Except(new[] { (BluetoothConnection)connection }));
            }

            if (connectionLost != null &&
                connectionLost.GetInvocationList().Length > 0 &&
                connection.GetType().Equals(typeof(TcpConnection)))
                connectionLost(connection, ConnectionType.TCP, closeReason);
            else if (connectionLost != null &&
                connectionLost.GetInvocationList().Length > 0 &&
                connection.GetType().Equals(typeof(UdpConnection)))
                connectionLost(connection, ConnectionType.UDP, closeReason);
            else if (connectionLost != null &&
                connection.GetType().Equals(typeof(BluetoothConnection)))
                connectionLost(connection, ConnectionType.Bluetooth, closeReason);
        }

        /// <summary>
        /// Stops the Bluetooth listener. No new bluetooth clients are able to connect to the server anymore.
        /// </summary>
        public void StopBluetoothListener()
        {
            if (IsBluetoothOnline) bluetoothListener.Stop();
            IsBluetoothOnline = !IsBluetoothOnline;
        }

        /// <summary>
        /// Stops the TCP listener. No new tcp clients are able to connect to the server anymore.
        /// </summary>
        public void StopTCPListener()
        {
            if (IsTCPOnline) tcpListener.Stop();
            IsTCPOnline = !IsTCPOnline;
        }

        /// <summary>
        /// Stops listening to bluetooth and tcp clients.
        /// </summary>
        public void Stop()
        {
            StopBluetoothListener();
            StopTCPListener();
        }

        /// <summary>
        /// Closes all the tcp and udp connections.
        /// </summary>
        public void CloseConnections(CloseReason reason)
        {
            CloseTCPConnections(reason);
            CloseUDPConnections(reason);
            CloseBluetoothConnections(reason);

            //Clear or reassign the connection containers.
            bluetoothConnections = new ConcurrentBag<BluetoothConnection>();
            connections.Clear();
        }

        /// <summary>
        /// Closes all the tcp connections.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public void CloseTCPConnections(CloseReason reason)
        {
            connections.Keys.ToList().ForEach(c => c.Close(reason));
        }

        /// <summary>
        /// Closes all the udp connections.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public void CloseUDPConnections(CloseReason reason)
        {
            connections.Values.ToList().ForEach(c => c.ForEach(b => b.Close(reason)));
        }

        /// <summary>
        /// Closes all the bluetooth connections.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public void CloseBluetoothConnections(CloseReason reason)
        {
            bluetoothConnections.ToList().ForEach(b => b.Close(reason));
        }

        /// <summary>
        /// Sends a broadcast to all the connected tcp connections.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public void TCP_BroadCast(Packet packet)
        {
            connections.Keys.ToList().ForEach(c => c.Send(packet));
        }

        /// <summary>
        /// Sends a broadcast to all the connected udp connections.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public void UDP_BroadCast(Packet packet)
        {
            connections.Values.ToList().ForEach(c => c.ForEach(b => b.Send(packet)));
        }

        /// <summary>
        /// Sends a broadcast to all the connected bluetooth connections.
        /// </summary>
        /// <param name="packet"></param>
        public void BLUETOOTH_BroadCast(Packet packet)
        {
            bluetoothConnections.ToList().ForEach(b => b.Send(packet));
        }

        protected virtual TcpConnection CreateTcpConnection(TcpClient tcpClient) => ConnectionFactory.CreateTcpConnection(tcpClient);

        public override string ToString()
        {
            return $"ServerConnectionContainer. IsOnline {IsTCPOnline}. EnableUDPConnection {AllowUDPConnections}. UDPConnectionLimit {UDPConnectionLimit}. AllowBluetoothConnections {AllowBluetoothConnections}. Connected TCP connections {connections.Count}.";
        }
    }
}