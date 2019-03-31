using Network.Enums;
using Network.Interfaces;
using Network.Packets;
using Network.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Network
{
    /// <summary>
    /// The connection container contains a tcp and x udp connections.
    /// It provides convenient methods to reduce the number of code lines which are needed to manage all the connections.
    /// By default one tcp and one udp connection will be created automatically.
    /// </summary>
    public class ClientConnectionContainer : ConnectionContainer, IPacketHandler, IDisposable
    {
        /// <summary>
        /// The reconnect timer. Invoked if we lose the connection.
        /// </summary>
        private Timer reconnectTimer;

        /// <summary>
        /// The connections we have to deal with.
        /// </summary>
        private TcpConnection tcpConnection;

        private UdpConnection udpConnection;

        /// <summary>
        /// If there is no connection yet, save the packets in this buffer.
        /// </summary>
        private List<Packet> sendSlowBuffer = new List<Packet>();

        private List<Packet> sendFastBuffer = new List<Packet>();
        private List<Tuple<Packet, object>> sendSlowObjectBuffer = new List<Tuple<Packet, object>>();
        private List<Tuple<Packet, object>> sendFastObjectBuffer = new List<Tuple<Packet, object>>();

        /// <summary>
        /// Cache all the handlers to apply them after we got a new connection.
        /// </summary>
        private PacketHandlerMap tcpPacketHandlerBackup = new PacketHandlerMap();

        private PacketHandlerMap udpPacketHandlerBackup = new PacketHandlerMap();
        private List<Tuple<Type, Delegate, object>> tcpPacketHandlerBuffer = new List<Tuple<Type, Delegate, object>>();
        private List<Tuple<Type, Delegate, object>> udpPacketHandlerBuffer = new List<Tuple<Type, Delegate, object>>();
        private List<Tuple<Type, Delegate>> tcpStaticPacketHandlerBuffer = new List<Tuple<Type, Delegate>>();
        private List<Tuple<Type, Delegate>> udpStaticPacketHandlerBuffer = new List<Tuple<Type, Delegate>>();
        private List<Tuple<Type, object>> tcpUnPacketHandlerBuffer = new List<Tuple<Type, object>>();
        private List<Tuple<Type, object>> udpUnPacketHandlerBuffer = new List<Tuple<Type, object>>();
        private List<Type> tcpStaticUnPacketHandlerBuffer = new List<Type>();
        private List<Type> udpStaticUnPacketHandlerBuffer = new List<Type>();

        /// <summary>
        /// Occurs when we get or lose a tcp or udp connection.
        /// </summary>
        private event Action<Connection, ConnectionType, CloseReason> connectionLost;

        private event Action<Connection, ConnectionType> connectionEstablished;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="port">The port.</param>
        internal ClientConnectionContainer(string ipAddress, int port)
            : base(ipAddress, port) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectionContainer"/> class.
        /// </summary>
        /// <param name="tcpConnection">The TCP connection.</param>
        /// <param name="udpConnection">The UDP connection.</param>
        internal ClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection)
            : base(tcpConnection.IPRemoteEndPoint.Address.ToString(), tcpConnection.IPRemoteEndPoint.Port)
        {
            this.tcpConnection = tcpConnection;
            this.udpConnection = udpConnection;
        }

        /// <summary>
        /// Initializes this instance and starts connecting to the endpoint.
        /// </summary>
        internal void Initialize()
        {
            reconnectTimer = new Timer();
            reconnectTimer.Elapsed += TryToConnect;
            TryConnect();
        }

        /// <summary>
        /// Gets or sets if this container should automatically reconnect to the endpoint if the connection has been closed.
        /// </summary>
        /// <value><c>true</c> if [automatic reconnect]; otherwise, <c>false</c>.</value>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Gets or sets the reconnect interval in [ms].
        /// </summary>
        /// <value>The reconnect interval.</value>
        public int ReconnectInterval { get; set; } = 2500;

        /// <summary>
        /// Gets the TCP connection.
        /// </summary>
        /// <value>The TCP connection.</value>
        public TcpConnection TcpConnection { get { return tcpConnection; } }

        /// <summary>
        /// Gets the UDP connections.
        /// </summary>
        /// <value>The UDP connections.</value>
        public UdpConnection UdpConnection { get { return udpConnection; } }

        /// <summary>
        /// Will be called if a TCP or an UDP connection has been successfully established.
        /// </summary>
        public event Action<Connection, ConnectionType> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

        /// <summary>
        /// Will be called if a TCP or an UDP connection has been lost.
        /// </summary>
        public event Action<Connection, ConnectionType, CloseReason> ConnectionLost
        {
            add { connectionLost += value; }
            remove { connectionLost -= value; }
        }

        /// <summary>
        /// Gets if the TCP connection is alive.
        /// </summary>
        /// <value>The is alive_ TCP.</value>
        public bool IsAlive_TCP
        {
            get
            {
                if (tcpConnection == null)
                    return false;
                return tcpConnection.IsAlive;
            }
        }

        /// <summary>
        /// Gets if the udp connection is alive.
        /// </summary>
        /// <value>The is alive_ UDP.</value>
        public bool IsAlive_UDP
        {
            get
            {
                if (udpConnection == null)
                    return false;
                return udpConnection.IsAlive;
            }
        }

        /// <summary>
        /// Gets if the TCP and udp connection is alive.
        /// </summary>
        /// <value>The is alive.</value>
        public bool IsAlive { get { return IsAlive_TCP && IsAlive_UDP; } }

        /// <summary>
        /// Tries to connect to the given endpoint.
        /// </summary>
        /// <param name="e">e.</param>
        /// <param name="sender">sender.</param>
        private void TryToConnect(object sender, ElapsedEventArgs e)
        {
            TryConnect();
        }

        /// <summary>
        /// Tries to connect to the given endpoint.
        /// </summary>
        private async void TryConnect()
        {
            if (reconnectTimer != null)
                reconnectTimer.Stop();
            if (tcpConnection == null || !tcpConnection.IsAlive)
                await OpenNewTCPConnection();
            if ((udpConnection == null || !udpConnection.IsAlive) && IsAlive_TCP)
                await OpenNewUDPConnection();
        }

        /// <summary>
        /// Registers a packetHandler for TCP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void TCP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.RegisterPacketHandler<P>(handler, obj);
            else tcpPacketHandlerBuffer.Add(new Tuple<Type, Delegate, object>(typeof(P), handler, obj));
        }

        /// <summary>
        /// UnRegisters a packetHandler for TCP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <exception cref="System.NotImplementedException"></exception>
        public void TCP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterStaticPacketHandler<P>();
            else tcpStaticUnPacketHandlerBuffer.Add(typeof(P));
        }

        /// <summary>
        /// UnRegisters a packetHandler for TCP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void TCP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.UnRegisterPacketHandler<P>(obj);
            else tcpUnPacketHandlerBuffer.Add(new Tuple<Type, object>(typeof(P), obj));
        }

        /// <summary>
        /// Registers a packetHandler for UDP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UDP_RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterStaticPacketHandler<P>(handler);
            else udpStaticPacketHandlerBuffer.Add(new Tuple<Type, Delegate>(typeof(P), handler));
        }

        /// <summary>
        /// Registers a packetHandler for UDP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UDP_RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.RegisterPacketHandler<P>(handler, obj);
            else udpPacketHandlerBuffer.Add(new Tuple<Type, Delegate, object>(typeof(P), handler, obj));
        }

        /// <summary>
        /// UnRegisters a packetHandler for UDP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UDP_UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterStaticPacketHandler<P>();
            else udpStaticUnPacketHandlerBuffer.Add(typeof(P));
        }

        /// <summary>
        /// UnRegisters a packetHandler for UDP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UDP_UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            if (IsAlive_UDP) udpConnection.UnRegisterPacketHandler<P>(obj);
            else udpUnPacketHandlerBuffer.Add(new Tuple<Type, object>(typeof(P), obj));
        }

        /// <summary>
        /// Registers a packetHandler for TCP and UDP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            TCP_RegisterStaticPacketHandler<P>(handler);
            UDP_RegisterStaticPacketHandler<P>(handler);
        }

        /// <summary>
        /// Registers a packetHandler for TCP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void TCP_RegisterStaticPacketHandler<P>(PacketReceivedHandler<P> handler) where P : Packet
        {
            if (IsAlive_TCP) tcpConnection.RegisterStaticPacketHandler<P>(handler);
            else tcpStaticPacketHandlerBuffer.Add(new Tuple<Type, Delegate>(typeof(P), handler));
        }

        /// <summary>
        /// Registers a packetHandler for TCP and UDP. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="P">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RegisterPacketHandler<P>(PacketReceivedHandler<P> handler, object obj) where P : Packet
        {
            TCP_RegisterPacketHandler<P>(handler, obj);
            UDP_RegisterPacketHandler<P>(handler, obj);
        }

        /// <summary>
        /// UnRegisters a packetHandler for TCP and UDP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <exception cref="System.NotImplementedException"></exception>
        public void DeregisterStaticPacketHandler<P>() where P : Packet
        {
            TCP_UnRegisterStaticPacketHandler<P>();
            UDP_UnRegisterStaticPacketHandler<P>();
        }

        /// <inheritdoc />
        [Obsolete("Use 'DeregisterStaticPacketHandler' instead.")]
        public void UnRegisterStaticPacketHandler<P>() where P : Packet
        {
            DeregisterStaticPacketHandler<P>();
        }

        /// <summary>
        /// UnRegisters a packetHandler for TCP and UDP. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="P">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void DeregisterPacketHandler<P>(object obj) where P : Packet
        {
            TCP_UnRegisterPacketHandler<P>(obj);
            UDP_UnRegisterPacketHandler<P>(obj);
        }

        /// <inheritdoc />
        [Obsolete("Use 'DeregisterPacketHandler' instead.")]
        public void UnRegisterPacketHandler<P>(object obj) where P : Packet
        {
            DeregisterPacketHandler<P>(obj);
        }

        /// <summary>
        /// Closes all connections which are bound to this object.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        /// <param name="callCloseEvent">If the instance should call the connectionLost event.</param>
        public void Shutdown(CloseReason closeReason, bool callCloseEvent = false)
        {
            if (IsAlive_TCP) tcpConnection.Close(closeReason, callCloseEvent);
            if (IsAlive_UDP) udpConnection.Close(closeReason, callCloseEvent);
        }

        /// <summary>
        /// Opens the new TCP connection and applies the already registered packet handlers.
        /// </summary>
        private async Task OpenNewTCPConnection()
        {
            Tuple<TcpConnection, ConnectionResult> result = await CreateTcpConnection();

            if (result.Item2 != ConnectionResult.Connected)
            {
                Reconnect();
                return;
            }

            tcpConnection = result.Item1;

            //Restore old state by adding old packets
            tcpConnection.RestorePacketHandler(tcpPacketHandlerBackup);
            //Restore new state by adding packets the user wanted to register while the connection was dead.
            tcpPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod("RegisterPacketHandler", BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(tcpConnection, new object[] { t.Item2, t.Item3 });
            });
            tcpStaticPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod("RegisterStaticPacketHandler", BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(tcpConnection, new object[] { t.Item2 });
            });
            tcpConnection.ConnectionClosed += (c, cc) =>
            {
                tcpPacketHandlerBackup = cc.BackupPacketHandler();
                connectionLost?.Invoke(tcpConnection, ConnectionType.TCP, c);
                Reconnect();
            };
            sendSlowBuffer.ForEach(tcpConnection.Send);
            sendSlowObjectBuffer.ForEach(p => tcpConnection.Send(p.Item1, p.Item2));
            //Restore new state by removing the packets the user wanted to unregister while the connection was dead.
            tcpUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod("DeregisterPacketHandler");
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t.Item1);
                unRegisterPacketHandler.Invoke(tcpConnection, new object[] { t.Item2 });
            });
            tcpStaticUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod("DeregisterStaticPacketHandler");
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t);
                unRegisterPacketHandler.Invoke(tcpConnection, null);
            });

            KnownTypes.ForEach(TcpConnection.AddExternalPackets);
            //Clear the buffers since we added and removed the packet types.
            sendSlowBuffer.Clear();
            sendSlowObjectBuffer.Clear();
            tcpPacketHandlerBuffer.Clear();
            tcpUnPacketHandlerBuffer.Clear();
            tcpStaticPacketHandlerBuffer.Clear();
            tcpStaticUnPacketHandlerBuffer.Clear();

            if (!tcpConnection.IsAlive) return; //Connection could already be dead because of the prePackets.
            connectionEstablished?.Invoke(tcpConnection, ConnectionType.TCP);
        }

        /// <summary>
        /// Opens the new UDP connection and applies the already registered packet handlers.
        /// </summary>
        private async Task OpenNewUDPConnection()
        {
            Tuple<UdpConnection, ConnectionResult> result = await CreateUdpConnection();

            if (result.Item2 != ConnectionResult.Connected)
            {
                Reconnect();
                return;
            }

            udpConnection = result.Item1;
            //Restore old state by adding old packets
            udpConnection.RestorePacketHandler(udpPacketHandlerBackup);
            //Restore new state by adding packets the user wanted to register while the connection was dead.
            udpPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(udpConnection, new object[2] { t.Item2, t.Item3 });
            });
            udpStaticPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo registerPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.RegisterStaticPacketHandler), BindingFlags.NonPublic | BindingFlags.Instance);
                registerPacketHandler = registerPacketHandler.MakeGenericMethod(t.Item1);
                registerPacketHandler.Invoke(udpConnection, new object[] { t.Item2 });
            });
            udpConnection.ConnectionClosed += (c, cc) =>
            {
                udpPacketHandlerBackup = cc.BackupPacketHandler();
                connectionLost?.Invoke(udpConnection, ConnectionType.UDP, c);
                Reconnect();
            };
            sendFastBuffer.ForEach(udpConnection.Send);
            sendFastObjectBuffer.ForEach(p => udpConnection.Send(p.Item1, p.Item2));
            //Restore new state by removing the packets the user wanted to unregister while the connection was dead.
            udpUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t.Item1);
                unRegisterPacketHandler.Invoke(udpConnection, new object[] { t.Item2 });
            });
            udpStaticUnPacketHandlerBuffer.ForEach(t =>
            {
                MethodInfo unRegisterPacketHandler = typeof(Connection).GetMethod(nameof(IPacketHandler.UnRegisterStaticPacketHandler));
                unRegisterPacketHandler = unRegisterPacketHandler.MakeGenericMethod(t);
                unRegisterPacketHandler.Invoke(udpConnection, null);
            });

            KnownTypes.ForEach(UdpConnection.AddExternalPackets);
            //Clear the buffers since we added and removed the packet types.
            sendFastBuffer.Clear();
            sendFastObjectBuffer.Clear();
            udpPacketHandlerBuffer.Clear();
            udpUnPacketHandlerBuffer.Clear();
            udpStaticPacketHandlerBuffer.Clear();
            udpStaticUnPacketHandlerBuffer.Clear();

            if (!UdpConnection.IsAlive) return; //Connection could already be dead because of the prePackets.
            connectionEstablished?.Invoke(udpConnection, ConnectionType.UDP);
        }

        /// <summary>
        /// Sends a ping over the TCP connection.
        /// </summary>
        public void SendPing()
        {
            if (tcpConnection != null && !tcpConnection.IsAlive)
                sendSlowBuffer.Add(new PingRequest());
            else tcpConnection.SendPing();
        }

        /// <summary>
        /// Sends and receives the packet async over TCP.
        /// </summary>
        /// <typeparam name="T">The type of the answer.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket
        {
            return await SendAsync<T>(packet, ConnectionType.TCP);
        }

        /// <summary>
        /// Sends and receives the packet async.
        /// </summary>
        /// <typeparam name="T">The type of the answer.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <param name="connectionType">Type of the connection to send it over.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        /// <exception cref="System.ArgumentException">The given enum doesn't exist</exception>
        public async Task<T> SendAsync<T>(Packet packet, ConnectionType connectionType) where T : ResponsePacket
        {
            if (connectionType == ConnectionType.TCP)
                return await SendSlowAsync<T>(packet);
            else if (connectionType == ConnectionType.UDP)
                return await SendFastAsync<T>(packet);
            else throw new ArgumentException("The given enum doesn't exist");
        }

        /// <summary>
        /// Sends and receives the packet async over TCP.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        public async Task<T> SendSlowAsync<T>(Packet packet) where T : ResponsePacket
        {
            if (IsAlive_TCP) return await tcpConnection.SendAsync<T>(packet);
            T response = Activator.CreateInstance<T>();
            response.State = PacketState.ConnectionNotAlive;
            return response;
        }

        /// <summary>
        /// Sends and receives the packet async over UDP.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet">The packet.</param>
        /// <returns>Task&lt;T&gt;.</returns>
        public async Task<T> SendFastAsync<T>(Packet packet) where T : ResponsePacket
        {
            if (IsAlive_UDP) return await udpConnection.SendAsync<T>(packet);
            T response = Activator.CreateInstance<T>();
            response.State = PacketState.ConnectionNotAlive;
            return response;
        }

        /// <summary>
        /// Sends a packet via. TCP or UDP depending on the type.
        /// The server wont be able to send an answer, since no instance object is given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="type">The transmission type.</param>
        public void Send(Packet packet, ConnectionType type)
        {
            if (type == ConnectionType.TCP)
                SendSlow(packet);
            else if (type == ConnectionType.UDP)
                SendFast(packet);
            else throw new ArgumentException("The given enum doesn't exist");
        }

        /// <summary>
        /// Sends a packet via. TCP by default.
        /// The server wont be able to send an answer, since no instance object is given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void Send(Packet packet)
        {
            SendSlow(packet);
        }

        /// <summary>
        /// Sends the given packet over the TCP connection.
        /// The server wont be able to send an answer, since no instance object is given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendSlow(Packet packet)
        {
            if (tcpConnection == null || !tcpConnection.IsAlive)
                sendSlowBuffer.Add(packet);
            else tcpConnection.Send(packet);
        }

        /// <summary>
        /// Sends the given packet over the UDP connection.
        /// The server wont be able to send an answer, since no instance object is given.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendFast(Packet packet)
        {
            if (udpConnection == null || !udpConnection.IsAlive)
                sendFastBuffer.Add(packet);
            else udpConnection.Send(packet);
        }

        /// <summary>
        /// Sends a packet via. TCP or UDP depending on the type.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The instance to receive an answer.</param>
        /// <param name="type">The transmission type.</param>
        /// <exception cref="System.ArgumentException">The given enum doesn't exist</exception>
        public void Send(Packet packet, object instance, ConnectionType type)
        {
            if (type == ConnectionType.TCP)
                SendSlow(packet, instance);
            else if (type == ConnectionType.UDP)
                SendFast(packet, instance);
            else throw new ArgumentException("The given enum doesn't exist");
        }

        /// <summary>
        /// Sends a packet via. TCP by default.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="instance">The instance to receive an answer.</param>
        public void Send(Packet packet, object instance)
        {
            SendSlow(packet, instance);
        }

        /// <summary>
        /// Sends the given packet over the TCP connection.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendSlow(Packet packet, object instance)
        {
            if (IsAlive_TCP) tcpConnection.Send(packet, instance);
            else sendSlowObjectBuffer.Add(new Tuple<Packet, object>(packet, instance));
        }

        /// <summary>
        /// Sends the given packet over the UDP connection.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendFast(Packet packet, object instance)
        {
            if (IsAlive_UDP) udpConnection.Send(packet, instance);
            else sendFastObjectBuffer.Add(new Tuple<Packet, object>(packet, instance));
        }

        /// <summary>
        /// Reconnects the TCP and/or the udp connection.
        /// </summary>
        /// <param name="forceReconnect">If AutoReconnect is disabled, force a reconnect by settings forceReconnect to true.</param>
        public void Reconnect(bool forceReconnect = false)
        {
            reconnectTimer.Stop();
            reconnectTimer.Interval = ReconnectInterval;

            if (forceReconnect || AutoReconnect)
                reconnectTimer.Start();
        }

        /// <summary>
        /// Creates a new TcpConnection.
        /// </summary>
        /// <returns>A TcpConnection.</returns>
        protected virtual async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnection() => await ConnectionFactory.CreateTcpConnectionAsync(IPAddress, Port);

        /// <summary>
        /// Creates a new UdpConnection from the existing tcpConnection.
        /// </summary>
        /// <returns>A UdpConnection.</returns>
        protected virtual async Task<Tuple<UdpConnection, ConnectionResult>> CreateUdpConnection() => await ConnectionFactory.CreateUdpConnectionAsync(tcpConnection);

        public override string ToString()
        {
            return $"ClientConnectionContainer. TCP is alive {IsAlive_TCP}. UDP is alive {IsAlive_UDP}. Server IPAddress {IPAddress} Port {Port.ToString()}";
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Dispose()
        {
            reconnectTimer.Dispose();
        }
    }
}