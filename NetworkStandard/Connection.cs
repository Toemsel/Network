#region Licence - LGPLv3
// ***********************************************************************
// Assembly         : Network
// Author           : Thomas
// Created          : 07-23-2015
//
// Last Modified By : Thomas
// Last Modified On : 08-05-2015
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
using Network.Attributes;
using Network.Enums;
using Network.Extensions;
using Network.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Network.Converter;
using Network.Interfaces;
using System.Threading.Tasks;
using Network.Async;

namespace Network
{
    /// <summary>
    /// Provides the basic methods a connection has to implement.
    /// It ensures the connectivity, is able to send pings and keeps track of the latency.
    /// Every connection instance has 3 threads:
    /// - (1) Send thread       -> Writes enqueued packets on the stream
    /// - (2) Read thread       -> Read bytes from the stream
    /// - (3) Invoke thread     -> Delegates the received packets to the given delegate.
    /// All 3 threads will be automatically aborted if the connection has been closed.
    /// After closing the connection, every packet in the send queue will be send before closing the connection.
    /// </summary>
    public abstract partial class Connection : IPacketHandler
    {
        /// <summary>
        /// Constants.
        /// </summary>
        protected const int PING_INTERVALL = 10000;

        /// <summary>
        /// True if this instance should send in a specific interval a keep alive packet, to ensure
        /// whether there is a connection or not. If set to [false] <see cref="RTT"/> and <see cref="Ping"/> wont be enabled/refreshed.
        /// </summary>
        private bool keepAlive = false;
        private bool writeLock = true;

        /// <summary>
        /// A fix hashCode that does not change, even if the most objects changed their values.
        /// </summary>
        private int hashCode;

        /// <summary>
        /// Is able to convert a packet into a byte array and back.
        /// </summary>
        private IPacketConverter packetConverter = new PacketConverter();

        /// <summary>
        /// A handler which will be invoked if this connection is dead.
        /// </summary>
        private event Action<CloseReason, Connection> connectionClosed;
        private event Action<TcpConnection, UdpConnection> connectionEstablished;
        private ConcurrentQueue<UdpConnection> pendingUDPConnections = new ConcurrentQueue<UdpConnection>();
        private ConcurrentQueue<Tuple<Packet, object>> pendingUnknownPackets = new ConcurrentQueue<Tuple<Packet, object>>();

        /// <summary>
        /// When this stopwatch reached the <see cref="TIMEOUT"/> the instance is going to send a ping request.
        /// </summary>
        private Stopwatch nextPingStopWatch = new Stopwatch();
        private Stopwatch currentPingStopWatch = new Stopwatch();

        /// <summary>
        /// This concurrent queue contains the received/send packets which we have to handle.
        /// </summary>
        private ConcurrentQueue<Packet> receivedPackets = new ConcurrentQueue<Packet>();
        private ConcurrentQueue<Tuple<Packet, object>> sendPackets = new ConcurrentQueue<Tuple<Packet, object>>();
        private ConcurrentQueue<Tuple<Packet, object>> writeLockBuffer = new ConcurrentQueue<Tuple<Packet, object>>();

        /// <summary>
        /// Events to save CPU time.
        /// </summary>
        private AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);
        private AutoResetEvent packetAvailableEvent = new AutoResetEvent(false);

        #region Threads
        private Thread readStreamThread;
        private Thread writeStreamThread;
        private Thread invokePacketThread;
        #endregion Threads

        /// <summary>
        /// Maps the type of a packet to their byte value.
        /// </summary>
        private BiDictionary<Type, ushort> typeByte = new BiDictionary<Type, ushort>();
        private int currentTypeByteIndex = 100; //The current index we are facing. Start with 100, since we have some network packets.
        /// <summary>
        /// Maps a request to their response.
        /// </summary>
        private static Dictionary<Type, Type> requestResponseMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Has to map the objects to their unique id and back.
        /// </summary>
        private ObjectMap objectMap = new ObjectMap();

        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        internal Connection()
        {
            //Set the hashCode of this instance.
            hashCode = this.GenerateUniqueHashCode();
            AddExternalPackets(Assembly.GetAssembly(typeof(Packet)));
        }

        /// <summary>
        /// Initializes the specified connection stream.
        /// </summary>
        /// <param name="connectionStream">The connection stream.</param>
        /// <param name="endPoint">The end point.</param>
        internal void Init()
        {
            InitAddons();

            readStreamThread = new Thread(ReadWork);
            readStreamThread.Priority = ThreadPriority.Normal;
            readStreamThread.Name = $"Read Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            readStreamThread.IsBackground = true;

            writeStreamThread = new Thread(WriteWork);
            writeStreamThread.Priority = ThreadPriority.Normal;
            writeStreamThread.Name = $"Write Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            writeStreamThread.IsBackground = true;

            invokePacketThread = new Thread(InvokeWork);
            invokePacketThread.Priority = ThreadPriority.Normal;
            invokePacketThread.Name = $"Invoke Thread {IPLocalEndPoint.AddressFamily.ToString()}";
            invokePacketThread.IsBackground = true;

            readStreamThread.Start();
            writeStreamThread.Start();
            invokePacketThread.Start();
        }

        /// <summary>
        /// External packets which also should be known by the network lib can be added with this function.
        /// All packets in the network lib are included automatically. A call is not essential, even if the used packets
        /// are not included in the network library. Manuell calls have to be invoked on the client and server side to avaid incompatible states.
        /// </summary>
        /// <param name="assembly">The assembly to search for included packets.</param>
        internal void AddExternalPackets(Assembly assembly)
        {
            assembly.GetTypes().ToList().Where(c => c.IsSubclassOf(typeof(Packet))).ToList().ForEach(p =>
            {
                if (typeByte.ContainsKeyA(p)) return; //Already in the dictionary.
                ushort packetId = (ushort)Interlocked.Increment(ref currentTypeByteIndex);
                Attribute packetTypeAttribute = p.GetCustomAttribute(typeof(PacketTypeAttribute));
                //Apply the local ID if there exist any.
                if (packetTypeAttribute != null) packetId = ((PacketTypeAttribute)packetTypeAttribute).Id;
                typeByte.Add(p, packetId);
            });

            assembly.GetTypes().ToList().Where(c => c.GetCustomAttributes(typeof(PacketRequestAttribute)).Count() > 0).ToList().
                ForEach(c =>
                {
                    PacketRequestAttribute requestAttribute = ((PacketRequestAttribute)c.GetCustomAttribute(typeof(PacketRequestAttribute)));
                    if (!requestResponseMap.ContainsKey(requestAttribute.RequestType))
                        requestResponseMap.Add(requestAttribute.RequestType, c);
                });
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is alive and able to communicate with the endpoint.
        /// </summary>
        /// <value><c>true</c> if this instance is alive; otherwise, <c>false</c>.</value>
        public bool IsAlive { get { return readStreamThread.IsAlive && writeStreamThread.IsAlive && invokePacketThread.IsAlive; } }

        /// <summary>
        /// Gets or sets if this instance should send in a specific interval a keep alive packet, to ensure
        /// whether there is a connection or not. If set to [false] <see cref="RTT"/> and <see cref="Ping"/> wont be refreshed automatically.
        /// </summary>
        /// <value>Keep alive or not.</value>
        public bool KeepAlive
        {
            get { return keepAlive; }
            set
            {
                keepAlive = value;
                ConfigPing(keepAlive);
            }
        }

        /// <summary>
        /// Gets or sets the timeout. If the connection does not receive any packet within the specified timeout, the connection will timeout.
        /// </summary>
        /// <value>The timeout.</value>
        public int TIMEOUT { get; protected set; } = 2500;

        /// <summary>
        /// Gets the round trip time.
        /// </summary>
        /// <value>The RTT.</value>
        public virtual long RTT { get; protected set; } = 0;

        /// <summary>
        /// Gets the ping.
        /// </summary>
        /// <value>The ping.</value>
        public virtual long Ping { get; protected set; } = 0;

        /// <summary>
        /// Gets or sets whenever sending a packet to flush the stream immediately.
        /// </summary>
        /// <value>Force to flush or not.</value>
        public bool ForceFlush { get; set; } = true;

        /// <summary>
        /// Gets or sets the performance of the network lib.
        /// The higher the sleep intervals, the slower the connection.
        /// </summary>
        /// <value>The performance.</value>
        public Performance Performance { get; set; } = Performance.Default;

        /// <summary>
        /// Gets the performance as an integer.
        /// </summary>
        /// <value>The int performance.</value>
        public int IntPerformance { get { return (int)Performance; } }

        /// <summary>
        /// Use your own packetConverter to serialize/deserialze objects.
        /// Take care that the internal packet structure should still remain the same:
        ///     1. [16bits]  packet type
        ///     2. [32bits]  packet length
        ///     3. [xxbits]  packet data
        /// The default packetConverter uses reflection to get and set data within objects.
        /// Using your own packetConverter could result in a higher throughput.
        /// </summary>
        public IPacketConverter PacketConverter
        {
            set { packetConverter = value; }
        }

        /// <summary>
        /// Gets or sets if this connection is still locked. If so, we are not able to send until we get green light to
        /// start sending packets.
        /// </summary>
        protected bool WriteLock
        {
            get { return writeLock; }
            private set
            {
                writeLock = value;

                while (!writeLock && writeLockBuffer.Count > 0)
                {
                    Tuple<Packet, object> currentPacket = null;
                    if (!writeLockBuffer.TryDequeue(out currentPacket))
                        continue;

                    //Send all the packets we have in the buffer.
                    Send(currentPacket.Item1, currentPacket.Item2);
                }
            }
        }

        /// <summary>
        /// Gets all the packets we are listening to.
        /// </summary>
        internal ObjectMap ObjectMapper { get { return objectMap; } }

        /// <summary>
        /// Restores the packetHandler. Can only be called if the internal packetHandler is empty.
        /// </summary>
        /// <param name="objectMap">The object map to restore.</param>
        internal void RestorePacketHandler(ObjectMap objectMap)
        {
            this.objectMap = objectMap;
            ObjectMapRefreshed();
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        public void RegisterStaticPacketHandler<T>(PacketReceivedHandler<T> handler) where T : Packet
        {
            objectMap.RegisterStaticPacketHandler<T>(handler);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="del">The delete.</param>
        internal void RegisterStaticPacketHandler<T>(Delegate del) where T : Packet
        {
            objectMap.RegisterStaticPacketHandler<T>(del);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="handler">The handler which should be invoked.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        public void RegisterPacketHandler<T>(PacketReceivedHandler<T> handler, object obj) where T : Packet
        {
            objectMap.RegisterPacketHandler<T>(handler, obj);
        }

        /// <summary>
        /// "RawData" packets will be forwarded to the desired delegate.
        /// </summary>
        /// <param name="key">A specific raw data key. Only raw data packets with the given key will be forwarded to the given delegate.</param>
        /// <param name="handler">The delegate to forward the packet to.</param>
        public void RegisterRawDataHandler(string key, PacketReceivedHandler<RawData> handler)
        {
            objectMap.RegisterStaticRawDataHandler(key, handler);
        }

        /// <summary>
        /// Registers a packetHandler. This handler will be invoked if this connection
        /// receives the given type.
        /// </summary>
        /// <typeparam name="T">The type we would like to receive.</typeparam>
        /// <param name="del">The delegate.</param>
        /// <param name="obj">The object which wants to receive the packet.</param>
        internal void RegisterPacketHandler<T>(Delegate del, object obj) where T : Packet
        {
            objectMap.RegisterPacketHandler<T>(del, obj);
        }

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        public void UnRegisterStaticPacketHandler<T>() where T : Packet
        {
            objectMap.UnRegisterStaticPacketHandler<T>();
        }

        /// <summary>
        /// UnRegisters a packetHandler. If this connection will receive the given type, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <typeparam name="T">The type we dont want to receive anymore.</typeparam>
        /// <param name="obj">The object which wants to receive the packet.</param>
        public void UnRegisterPacketHandler<T>(object obj) where T : Packet
        {
            objectMap.UnRegisterPacketHandler<T>(obj);
        }

        /// <summary>
        /// UnRegisters a rawData delegate. If this connection will receive a raw data packet with the given key, it will be ignored,
        /// because there is no handler to invoke anymore.
        /// </summary>
        /// <param name="key">The key who is representing a raw data packet.</param>
        public void UnRegisterRawDataHandler(string key)
        {
            objectMap.UnRegisterStaticRawDataHandler(key);
        }

        /// <summary>
        /// Adds or removes an action which will be invoked if the network dies.
        /// </summary>
        public event Action<CloseReason, Connection> ConnectionClosed
        {
            add { connectionClosed += value; }
            remove { connectionClosed -= value; }
        }

        /// <summary>
        /// Adds or remove an action which will be invoked if the connection
        /// created a new UDP connection. The delivered tcpConnection represents the tcp connection
        /// which was in charge of the new establishment.
        /// </summary>
        public event Action<TcpConnection, UdpConnection> ConnectionEstablished
        {
            add { connectionEstablished += value; }
            remove { connectionEstablished -= value; }
        }

        /// <summary>
        /// Configurations the ping and rtt timers.
        /// </summary>
        private void ConfigPing(bool enable)
        {
#if !DEBUG
            if (enable) nextPingStopWatch.Restart();
            else nextPingStopWatch.Reset();
#endif
        }

        #region Sending
        /// <summary>
        /// Sends a ping if there is no ping request already running.
        /// </summary>
        public void SendPing()
        {
            if (currentPingStopWatch.IsRunning) return;
            nextPingStopWatch.Reset();
            currentPingStopWatch.Restart();
            Send(new PingRequest());
        }

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// You wont be able to receive an answer, because no calling object is given.
        /// Suitable for static classes, response packets or basic packets without any inheritance.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public void Send(Packet packet)
        {
            Send(packet, null);
        }

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// You are able to receive an answer. Iff the packet you send is a request packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="instance">The instance who called this method.</param>
        public void Send(Packet packet, object instance)
        {
            Send(packet, instance, false);
        }

        /// <summary>
        /// Converts the given packet into a binary array and sends it async to the client's endpoint.
        /// You are able to receive an answer. Iff the packet you send is a request packet.
        /// </summary>
        /// <typeparam name="T">The type of the expected answer.</typeparam>
        /// <param name="packet">The packet to send.</param>
        /// <returns>T.</returns>
        public async Task<T> SendAsync<T>(Packet packet) where T : ResponsePacket
        {
            return await new AsyncReceiver().Send<T>(packet, this);
        }

        /// <summary>
        /// Converts the given packet into a binary array and sends it to the client's endpoint.
        /// </summary>
        /// <param name="packet">The packet.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="ignoreWriteLock">if set to <c>true</c> [ignore write lock].</param>
        internal void Send(Packet packet, object instance, bool ignoreWriteLock)
        {
            //Ensure that everyone is aware of that packetType.
            if (!typeByte.ContainsKeyA(packet.GetType()) || pendingUnknownPackets.Any(p => p.Item1.GetType().Assembly.Equals(packet.GetType().Assembly)))
            {
                AddExternalPackets(packet.GetType().Assembly);
                pendingUnknownPackets.Enqueue(new Tuple<Packet, object>(packet, instance));
                Send(new AddPacketTypeRequest(packet.GetType().Assembly.FullName));
                return; //Wait till we receive green light.
            }

            if (WriteLock && !ignoreWriteLock)
            {
                writeLockBuffer.Enqueue(new Tuple<Packet, object>(packet, instance));
            }
            else
            {
                sendPackets.Enqueue(new Tuple<Packet, object>(packet, instance));
                dataAvailableEvent.Set();
            }
            #endregion Sending
        }

        #region Threads
        /// <summary>
        /// Reads the bytes from the stream.
        /// </summary>
        private void ReadWork()
        {
            try
            {
                while (true)
                {
                    ushort packetType = BitConverter.ToUInt16(ReadBytes(2), 0);
                    int packetLength = BitConverter.ToInt32(ReadBytes(4), 0);
                    byte[] packetData = ReadBytes(packetLength);

                    if(!typeByte.ContainsKeyB(packetType))
                    {
                        //Theoretically it is not possible that we receive a packet
                        //which is not known, since all the packets need to pass a certification.
                        //But if the UDP connection sends something and the AddPacketTypeRequest get lost
                        //this case is going to happen.
                        HandleUnknownPacket();
                        continue;
                    }
                    Packet receivedPacket = packetConverter.GetPacket(typeByte[packetType], packetData);
                    receivedPackets.Enqueue(receivedPacket);
                    receivedPacket.Size = packetLength;
                    packetAvailableEvent.Set();

                    logger.LogInComingPacket(packetData, receivedPacket);
                }
            }
            catch (ThreadAbortException) { return; }
            catch (Exception exception)
            {
                logger.Log("Reading packet from stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.ReadPacketThreadException);
        }

        /// <summary>
        /// Writes the packets to the stream.
        /// </summary>
        private void WriteWork()
        {
            try
            {
                while (true)
                {
                    //Wait till we have something to send.
                    dataAvailableEvent.WaitOne();

                    WriteSubWork();

                    //Check if the client is still alive.
                    if (KeepAlive && nextPingStopWatch.ElapsedMilliseconds >= PING_INTERVALL)
                    {
                        SendPing();
                    }
                    else if (currentPingStopWatch.ElapsedMilliseconds >= TIMEOUT &&
                        currentPingStopWatch.ElapsedMilliseconds != 0)
                    {
                        ConfigPing(KeepAlive);
                        currentPingStopWatch.Reset();
                        CloseHandler(CloseReason.Timeout);
                    }
                }
            }
            catch (ThreadAbortException) { return; }
            catch(Exception exception)
            {
                logger.Log("Write object on stream", exception, LogLevel.Exception);
            }

            CloseHandler(CloseReason.WritePacketThreadException);
        }

        /// <summary>
        /// This thread checks for new packets in the queue and delegates them
        /// to the desired delegates, if given.
        /// </summary>
        private void InvokeWork()
        {
            try
            {
                while (true)
                {
                    //Wait till we receive a packet.
                    packetAvailableEvent.WaitOne();

                    while (receivedPackets.Count > 0)
                    {
                        Packet toDelegate = null;
                        if (!receivedPackets.TryDequeue(out toDelegate))
                            continue;

                        toDelegate.BeforeReceive();
                        HandleDefaultPackets(toDelegate);
                    }
                }
            }
            catch (ThreadAbortException) { return; }
            catch(Exception) { }

            CloseHandler(CloseReason.InvokePacketThreadException);
        }
        #endregion Threads

        /// <summary>
        /// Writes the packets to the stream.
        /// </summary>
        private void WriteSubWork()
        {
            while (sendPackets.Count > 0)
            {
                Tuple<Packet, object> packetWithObject = null;
                if (!sendPackets.TryDequeue(out packetWithObject))
                    continue;

                Packet packet = packetWithObject.Item1;

                //Insert the ID into the packet if it is an request packet.
                if (packet.GetType().IsSubclassOf(typeof(RequestPacket)) && packetWithObject.Item2 != null)
                    packet.ID = objectMap[requestResponseMap[packet.GetType()], packetWithObject.Item2];

                //Prepare some data in the packet.
                packet.BeforeSend();

                /*              Packet structure:
                                1. [16bits] packet type
                                2. [32bits] packet length
                                3. [xxbits] packet data                 */

                byte[] packetData = packetConverter.GetBytes(packet);
                byte[] packetLength = BitConverter.GetBytes(packetData.Length);
                byte[] packetByte = new byte[2 + packetLength.Length + packetData.Length];

                packetByte[0] = (byte)(typeByte[packet.GetType()]);
                packetByte[1] = (byte)(typeByte[packet.GetType()] >> 8);
                Array.Copy(packetLength, 0, packetByte, 2, packetLength.Length);
                Array.Copy(packetData, 0, packetByte, 2 + packetLength.Length, packetData.Length);
                WriteBytes(packetByte);

                logger.LogOutgoingPacket(packetData, packet);
            }
        }

        /// <summary>
        /// Handle the network's packets.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        private void HandleDefaultPackets(Packet packet)
        {
            if (packet.GetType().Equals(typeof(PingRequest)))
            {
                Send(new PingResponse());
                return;
            }
            else if (packet.GetType().Equals(typeof(PingResponse)))
            {
                long elapsedTime = currentPingStopWatch.ElapsedMilliseconds;
                currentPingStopWatch.Reset();
                nextPingStopWatch.Restart();

                Ping = elapsedTime / 2;
                RTT = elapsedTime;
                return;
            }
            else if (packet.GetType().Equals(typeof(CloseRequest)))
            {
                CloseRequest closeRequest = (CloseRequest)packet;
                ExternalClose(closeRequest.CloseReason);
                return;
            }
            else if (packet.GetType().Equals(typeof(EstablishUdpRequest)))
            {
                EstablishUdpRequest establishUdpRequest = (EstablishUdpRequest)packet;
                IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, GetFreePort());
                Send(new EstablishUdpResponse(udpEndPoint.Port, establishUdpRequest));
                UdpConnection udpConnection = new UdpConnection(new UdpClient(udpEndPoint),
                    new IPEndPoint(IPRemoteEndPoint.Address, establishUdpRequest.UdpPort), true);
                pendingUDPConnections.Enqueue(udpConnection);
                connectionEstablished?.Invoke((TcpConnection)this, udpConnection);
                return;
            }
            else if (packet.GetType().Equals(typeof(EstablishUdpResponseACK)))
            {
                UdpConnection udpConnection = null;
                while (!pendingUDPConnections.TryDequeue(out udpConnection))
                    Thread.Sleep(IntPerformance);
                udpConnection.AcknowledgePending = false;
                return;
            }
            else if (packet.GetType().Equals(typeof(DisableWriteLock)))
            {
                WriteLock = false;
                return;
            }
            else if(packet.GetType().Equals(typeof(AddPacketTypeRequest)))
            {
                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == ((AddPacketTypeRequest)packet).AssemblyName).SingleOrDefault();
                if (assembly == null) CloseHandler(CloseReason.AssemblyDoesNotExist);
                else AddExternalPackets(assembly);
                Send(new AddPacketTypeResponse(typeByte.BElements, (AddPacketTypeRequest)packet));
                return;
            }
            else if (packet.GetType().Equals(typeof(AddPacketTypeResponse)))
            {
                List<Tuple<Packet, object>> internalSendQueue = new List<Tuple<Packet, object>>();
                AddPacketTypeResponse addPacketTypeResponse = (AddPacketTypeResponse)packet;
                //Remove all packets of this type and send them :)
                while (true)
                {
                    Tuple<Packet, object> toSend = null;
                    while (!pendingUnknownPackets.TryPeek(out toSend) && pendingUnknownPackets.Count > 0)
                        Thread.Sleep(IntPerformance); //Wait till we got a result.

                    //If the other connection contains that packet, send it.
                    if (toSend != null && addPacketTypeResponse.LocalDict.Contains(typeByte[toSend.Item1.GetType()]))
                    {
                        while (!pendingUnknownPackets.TryDequeue(out toSend))
                            Thread.Sleep(IntPerformance); //Wait till we got a result.
                        internalSendQueue.Add(new Tuple<Packet, object>(toSend.Item1, toSend.Item2));
                        continue;
                    }

                    //Now the pendingUnknownPackets queue doesn't contain those elements any more.
                    internalSendQueue.ForEach(i => Send(i.Item1, i.Item2));
                    return;
                }
            }
            //Receiving raw data from the connection.
            else if(packet.GetType().Equals(typeof(RawData)))
            {
                RawData rawData = (RawData)packet;
                if(objectMap[rawData.Key] == null)
                    logger.Log($"RawData packet has no listener. Key: {rawData.Key}", LogLevel.Warning);
                else objectMap[rawData.Key].DynamicInvoke(new object[] { packet, this });
                return;
            }

            try
            {
                if(packet.GetType().IsSubclassOf(typeof(ResponsePacket)) && objectMap[packet.ID] != null)
                    objectMap[packet.ID].DynamicInvoke(new object[] { packet, this });
                else if(packet.GetType().IsSubclassOf(typeof(RequestPacket)) && objectMap[packet.GetType()] != null)
                    objectMap[packet.GetType()].DynamicInvoke(new object[] { packet, this });
                else CloseHandler(CloseReason.UnknownPacket);
            }
            catch(Exception exception)
            {
                logger.Log("Provided delegate contains a bug. Packet invocation thread crashed.", exception, LogLevel.Exception);
            }
        }

        /// <summary>
        /// The remote endpoint closed the connection.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        internal void ExternalClose(CloseReason closeReason)
        {
            writeStreamThread.AbortSave();
            readStreamThread.AbortSave();
            connectionClosed?.Invoke(closeReason, this);
            invokePacketThread.AbortSave();
            CloseSocket();
        }

        /// <summary>
        /// Closes this connection, but still sends the data on the stream to the bound endpoint.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        /// <param name="callCloseEvent">If the instance should call the connectionLost event.</param>
        public void Close(CloseReason closeReason, bool callCloseEvent = false)
        {
            //Check if this connection is already dead. If so, there is no need to
            //handle an exception or anything else.
            if (!IsAlive)
                return;

            try
            {
                Send(new CloseRequest(closeReason), true);
                WriteSubWork(); //Force to write the remaining packets.
            }
            catch { }

            if (callCloseEvent)
                connectionClosed?.Invoke(closeReason, this);

            writeStreamThread.AbortSave();
            readStreamThread.AbortSave();
            invokePacketThread.AbortSave();
            CloseSocket();
        }

        /// <summary>
        /// Unlocks the remote connection so that he is able to send packets.
        /// </summary>
        public void UnlockRemoteConnection()
        {
            Send(new DisableWriteLock(), null, true);
        }

        /// <summary>
        /// Unlocks the local connection so that this connection is able to send.
        /// </summary>
        internal void UnlockLocalConnection()
        {
            WriteLock = false;
        }

        /// <summary>
        /// Gets the next free port.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected int GetFreePort()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Handles the unknown packet.
        /// </summary>
        protected abstract void HandleUnknownPacket();

        /// <summary>
        /// The objectMap has been refreshed.
        /// </summary>
        public virtual void ObjectMapRefreshed() { }

        /// <summary>
        /// Gets or sets the time to live for the tcp connection.
        /// </summary>
        /// <value>The TTL.</value>
        public abstract short TTL { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [dual mode]. (Ipv6 + Ipv4)
        /// </summary>
        /// <value><c>true</c> if [dual mode]; otherwise, <c>false</c>.</value>
        public abstract bool DualMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Connection"/> is allowed to fragment the frames.
        /// </summary>
        /// <value><c>true</c> if fragment; otherwise, <c>false</c>.</value>
        public abstract bool Fragment { get; set; }

        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        protected abstract byte[] ReadBytes(int amount);

        /// <summary>
        /// Writes bytes to the stream.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        protected abstract void WriteBytes(byte[] bytes);

        /// <summary>
        /// Handles if the connection should be closed, based on the reason.
        /// </summary>
        /// <param name="closeReason">The close reason.</param>
        protected abstract void CloseHandler(CloseReason closeReason);

        /// <summary>
        /// The hop limit. This is compareable to the Ipv4 TTL.
        /// </summary>
        public abstract int HopLimit { get; set; }

        /// <summary>
        /// Gets or sets if the packet should be send with or without any delay.
        /// If disabled, no data will be buffered at all and sent immediately to it's destination.
        /// There is no guarantee that the network performance will be increased.
        /// </summary>
        public abstract bool NoDelay { get; set; }

        /// <summary>
        /// Gets or sets if the packet should be sent directly to its destination or not.
        /// </summary>
        public abstract bool IsRoutingEnabled { get; set; }

        /// <summary>
        /// Gets or sets if it should bypass hardware.
        /// </summary>
        public abstract bool UseLoopback { get; set; }

        /// <summary>
        /// Gets the ip address's local endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public abstract IPEndPoint IPLocalEndPoint { get; }

        /// <summary>
        /// Gets the ip address's remote endpoint of this connection.
        /// </summary>
        /// <value>The ip end point.</value>
        public abstract IPEndPoint IPRemoteEndPoint { get; }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        protected abstract void CloseSocket();

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return hashCode;
        }

        /// <summary>
        /// Value of the connection.
        /// </summary>
        /// <returns>Overall data about the connection.</returns>
        public override string ToString()
        {
            return $"Local: {IPLocalEndPoint?.ToString()} Remote: {IPRemoteEndPoint?.ToString()}";
        }
    }
}