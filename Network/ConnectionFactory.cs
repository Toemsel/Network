#if NET46

using InTheHand.Net.Sockets;
using Network.Bluetooth;

#endif

using Network.RSA;
using System;
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
        /// A connection was established successfully.
        /// </summary>
        Connected,

        /// <summary>
        /// A connection couldn't be established. The IP, port and firewall might have to be checked.
        /// </summary>
        Timeout,

        /// <summary>
        /// Could not establish a UDP connection as the parent TCP connection is not alive.
        /// </summary>
        TCPConnectionNotAlive
    }

#if NET46
    /// <summary>
    /// Factory for instantiating <see cref="TcpConnection"/>s, <see cref="UdpConnection"/>s, and <see cref="BluetoothConnection"/>s
    /// as well as <see cref="ClientConnectionContainer"/>s and <see cref="ServerConnectionContainer"/>s (and their secure variants).
    /// </summary>
#elif NETSTANDARD2_0
    /// <summary>
    /// Factory for instantiating <see cref="TcpConnection"/>s and <see cref="UdpConnection"/>s as well as
    /// <see cref="ClientConnectionContainer"/>s and <see cref="ServerConnectionContainer"/>s (and their secure variants).
    /// </summary>
#endif

    public static class ConnectionFactory
    {
        #region Variables

        /// <summary>
        /// Timeout interval in milliseconds.
        /// </summary>
        public const int CONNECTION_TIMEOUT = 8000;

#if NET46

        /// <summary>
        /// The GUID of this assembly, needed for bluetooth connections.
        /// </summary>
        internal static Guid GUID;

#endif

        #endregion Variables

        #region Constructors

#if NET46

        /// <summary>
        /// Initializes a new instance of the static <see cref="ConnectionFactory"/> class. Sets the <see cref="GUID"/>.
        /// </summary>
        static ConnectionFactory()
        {
            GUID = Assembly.GetAssembly(typeof(Connection)).GetType().GUID;
        }

#endif

        #endregion Constructors

        #region Methods

        #region Bluetooth Connection Factory

#if NET46

        /// <summary>
        /// Finds and returns all Bluetooth devices that are within range of the current device, and are discoverable.
        /// </summary>
        /// <returns>All discoverable Bluetooth devices.</returns>
        public static DeviceInfo[] GetBluetoothDevices() { return DeviceInfo.GenerateDeviceInfos(new BluetoothClient().DiscoverDevicesInRange()); }

        /// <summary>
        /// Asynchronously finds and returns all Bluetooth devices that are within range of the current device, and are discoverable.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the promise of a <see cref="DeviceInfo"/> array on completion.
        /// </returns>
        public static async Task<DeviceInfo[]> GetBluetoothDevicesAsync()
        {
            return await Task.Factory.StartNew(() => DeviceInfo.GenerateDeviceInfos(new BluetoothClient().DiscoverDevices()));
        }

        /// <summary>
        /// Creates a <see cref="BluetoothConnection"/> and connects it.
        /// </summary>
        /// <param name="bluetoothDeviceInfo">The device information for the remote Bluetooth device.</param>
        /// <returns>A tuple with the <see cref="ConnectionResult"/> and created <see cref="BluetoothConnection"/>.</returns>
        public static Tuple<ConnectionResult, BluetoothConnection> CreateBluetoothConnection(DeviceInfo bluetoothDeviceInfo)
        {
            BluetoothConnection bluetoothConnection = new BluetoothConnection(bluetoothDeviceInfo);
            ConnectionResult result = bluetoothConnection.TryConnect().Result;
            return new Tuple<ConnectionResult, BluetoothConnection>(result, bluetoothConnection);
        }

        /// <summary>
        /// Asynchronously creates a <see cref="BluetoothConnection"/> and connects it.
        /// </summary>
        /// <param name="bluetoothDeviceInfo">The device information for the remote Bluetooth device.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> representing the asynchronous operation, with the promise of a tuple with the
        /// <see cref="ConnectionResult"/> and created <see cref="BluetoothConnection"/> on completion.
        /// </returns>
        public static async Task<Tuple<ConnectionResult, BluetoothConnection>> CreateBluetoothConnectionAsync(DeviceInfo bluetoothDeviceInfo)
        {
            BluetoothConnection bluetoothConnection = new BluetoothConnection(bluetoothDeviceInfo);
            ConnectionResult result = await bluetoothConnection.TryConnect();
            return new Tuple<ConnectionResult, BluetoothConnection>(result, bluetoothConnection);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BluetoothConnection"/> with the given client.
        /// </summary>
        /// <param name="bluetoothClient">The client to create a connection with.</param>
        /// <returns>The created <see cref="BluetoothConnection"/>.</returns>
        internal static BluetoothConnection CreateBluetoothConnection(BluetoothClient bluetoothClient)
        {
            return new BluetoothConnection(bluetoothClient);
        }

#endif

        #endregion Bluetooth Connection Factory

        #region TCP Connection Factory

        /// <summary>
        /// Creates a <see cref="TcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>The created <see cref="TcpConnection"/>.</returns>
        public static TcpConnection CreateTcpConnection(string ipAddress, int port, out ConnectionResult connectionResult)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateTcpConnectionAsync(ipAddress, port).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <param name="keySize">The size to use for the RSA keys.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        public static TcpConnection CreateSecureTcpConnection(string ipAddress, int port, out ConnectionResult connectionResult, int keySize = 2048)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateSecureTcpConnectionAsync(ipAddress, port, RSAKeyGeneration.Generate(keySize)).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <param name="keySize">The size to use for the RSA keys.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        public static TcpConnection CreateSecureTcpConnection(string ipAddress, int port, string publicKey, string privateKey, out ConnectionResult connectionResult, int keySize = 2048)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateSecureTcpConnectionAsync(ipAddress, port, publicKey, privateKey, keySize).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        public static TcpConnection CreateSecureTcpConnection(string ipAddress, int port, RSAPair rsaPair, out ConnectionResult connectionResult)
        {
            Tuple<TcpConnection, ConnectionResult> tcpConnection = CreateSecureTcpConnectionAsync(ipAddress, port, rsaPair).Result;
            connectionResult = tcpConnection.Item2;
            return tcpConnection.Item1;
        }

        /// <summary>
        /// Asynchronously creates a <see cref="TcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="TcpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateTcpConnectionAsync(string ipAddress, int port)
        {
            TcpClient tcpClient = new TcpClient();
            Task timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
            Task connectTask = Task.Factory.StartNew(() => tcpClient.Connect(ipAddress, port));
            if (await Task.WhenAny(timeoutTask, connectTask).ConfigureAwait(false) != timeoutTask && tcpClient.Connected)
                return new Tuple<TcpConnection, ConnectionResult>(new TcpConnection(tcpClient), ConnectionResult.Connected);

            return new Tuple<TcpConnection, ConnectionResult>(null, ConnectionResult.Timeout);
        }

        /// <summary>
        /// Asynchronously creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureTcpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateSecureTcpConnectionAsync(string ipAddress, int port, int keySize = 2048) =>
            await CreateSecureTcpConnectionAsync(ipAddress, port, RSAKeyGeneration.Generate(keySize));

        /// <summary>
        /// Asynchronously creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureTcpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateSecureTcpConnectionAsync(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048) =>
            await CreateSecureTcpConnectionAsync(ipAddress, port, new RSAPair(publicKey, privateKey, keySize));

        /// <summary>
        /// Asynchronously creates a <see cref="SecureTcpConnection"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureTcpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        public static async Task<Tuple<TcpConnection, ConnectionResult>> CreateSecureTcpConnectionAsync(string ipAddress, int port, RSAPair rsaPair)
        {
            TcpClient tcpClient = new TcpClient();
            Task timeoutTask = Task.Delay(CONNECTION_TIMEOUT);
            Task connectTask = Task.Factory.StartNew(() => tcpClient.Connect(ipAddress, port));
            if (await Task.WhenAny(timeoutTask, connectTask).ConfigureAwait(false) != timeoutTask && tcpClient.Connected)
                return new Tuple<TcpConnection, ConnectionResult>(new SecureTcpConnection(rsaPair, tcpClient), ConnectionResult.Connected);

            return new Tuple<TcpConnection, ConnectionResult>(null, ConnectionResult.Timeout);
        }

        /// <summary>
        /// Creates a <see cref="TcpConnection"/> from the given <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to wrap.</param>
        /// <returns>The created <see cref="TcpConnection"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpClient"/>s socket is not connected.</exception>
        public static TcpConnection CreateTcpConnection(TcpClient tcpClient)
        {
            if (!tcpClient.Connected) throw new ArgumentException("Socket is not connected.");
            return new TcpConnection(tcpClient);
        }

        /// <summary>
        /// Creates a <see cref="SecureTcpConnection"/> from the given <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to wrap.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpClient"/>s socket is not connected.</exception>
        public static TcpConnection CreateSecureTcpConnection(TcpClient tcpClient, string publicKey, string privateKey, int keySize = 2048) =>
            CreateSecureTcpConnection(tcpClient, new RSAPair(publicKey, privateKey, keySize));

        /// <summary>
        /// Creates a <see cref="SecureTcpConnection"/> from the given <see cref="TcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The <see cref="TcpClient"/> to wrap.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <returns>The created <see cref="SecureTcpConnection"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpClient"/>s socket is not connected.</exception>
        public static TcpConnection CreateSecureTcpConnection(TcpClient tcpClient, RSAPair rsaPair)
        {
            if (!tcpClient.Connected) throw new ArgumentException("Socket is not connected.");
            return new SecureTcpConnection(rsaPair, tcpClient);
        }

        #endregion TCP Connection Factory

        #region UDP Connection Factory

        /// <summary>
        /// Creates a <see cref="UdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="UdpConnection"/>.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>The created <see cref="UdpConnection"/>.</returns>
        public static UdpConnection CreateUdpConnection(TcpConnection tcpConnection, out ConnectionResult connectionResult)
        {
            Tuple<UdpConnection, ConnectionResult> connectionRequest = CreateUdpConnectionAsync(tcpConnection).Result;
            connectionResult = connectionRequest.Item2;
            return connectionRequest.Item1;
        }

        /// <summary>
        /// Creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureUdpConnection"/>.</returns>
        public static UdpConnection CreateSecureUdpConnection(TcpConnection tcpConnection, out ConnectionResult connectionResult, int keySize = 2048) =>
            CreateSecureUdpConnection(tcpConnection, RSAKeyGeneration.Generate(keySize), out connectionResult);

        /// <summary>
        /// Creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureUdpConnection"/>.</returns>
        public static UdpConnection CreateSecureUdpConnection(TcpConnection tcpConnection, string publicKey, string privateKey, out ConnectionResult connectionResult, int keySize = 2048) =>
            CreateSecureUdpConnection(tcpConnection, new RSAPair(publicKey, privateKey, keySize), out connectionResult);

        /// <summary>
        /// Creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <param name="connectionResult">The connection result.</param>
        /// <returns>The created <see cref="SecureUdpConnection"/>.</returns>
        public static UdpConnection CreateSecureUdpConnection(TcpConnection tcpConnection, RSAPair rsaPair, out ConnectionResult connectionResult)
        {
            Tuple<UdpConnection, ConnectionResult> connectionRequest = CreateSecureUdpConnectionAsync(tcpConnection, rsaPair).Result;
            connectionResult = connectionRequest.Item2;
            return connectionRequest.Item1;
        }

        /// <summary>
        /// Asynchronously creates a <see cref="UdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="UdpConnection"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="UdpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        /// <exception cref="ArgumentException">The given <see cref="TcpConnection"/> isn't connected.</exception>
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
        /// Asynchronously creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureUdpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        /// <exception cref="ArgumentException">The given <see cref="TcpConnection"/> isn't connected.</exception>
        public static async Task<Tuple<UdpConnection, ConnectionResult>> CreateSecureUdpConnectionAsync(TcpConnection tcpConnection, int keySize = 2048) =>
            await CreateSecureUdpConnectionAsync(tcpConnection, RSAKeyGeneration.Generate(keySize));

        /// <summary>
        /// Asynchronously creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureUdpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        /// <exception cref="ArgumentException">The given <see cref="TcpConnection"/> isn't connected.</exception>
        public static async Task<Tuple<UdpConnection, ConnectionResult>> CreateSecureUdpConnectionAsync(TcpConnection tcpConnection, string publicKey, string privateKey, int keySize = 2048) =>
            await CreateSecureUdpConnectionAsync(tcpConnection, new RSAPair(publicKey, privateKey, keySize));

        /// <summary>
        /// Asynchronously creates a <see cref="SecureUdpConnection"/> with the given parent <see cref="TcpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> via which to connect the <see cref="SecureUdpConnection"/>.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation with the promise of a tuple holding the created
        /// <see cref="SecureUdpConnection"/> and <see cref="ConnectionResult"/> on completion.
        /// </returns>
        /// <exception cref="ArgumentException">The given <see cref="TcpConnection"/> isn't connected.</exception>
        public static async Task<Tuple<UdpConnection, ConnectionResult>> CreateSecureUdpConnectionAsync(TcpConnection tcpConnection, RSAPair rsaPair)
        {
            UdpConnection udpConnection = null;
            ConnectionResult connectionResult = ConnectionResult.Connected;
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            cancellationToken.CancelAfter(CONNECTION_TIMEOUT);
            if (tcpConnection == null || !tcpConnection.IsAlive)
                return new Tuple<UdpConnection, ConnectionResult>(udpConnection, ConnectionResult.TCPConnectionNotAlive);
            tcpConnection.EstablishUdpConnection((localEndPoint, RemoteEndPoint) => udpConnection = new SecureUdpConnection(new UdpClient(localEndPoint), RemoteEndPoint, rsaPair));
            while (udpConnection == null && !cancellationToken.IsCancellationRequested) await Task.Delay(25);
            if (udpConnection == null && cancellationToken.IsCancellationRequested) connectionResult = ConnectionResult.Timeout;
            return new Tuple<UdpConnection, ConnectionResult>(udpConnection, connectionResult);
        }

        #endregion UDP Connection Factory

        #region Client Connection Container Factory

        /// <summary>
        /// Creates a <see cref="ClientConnectionContainer"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>The created <see cref="ClientConnectionContainer"/>.</returns>
        public static ClientConnectionContainer CreateClientConnectionContainer(string ipAddress, int port)
        {
            var clientConnectionContainer = new ClientConnectionContainer(ipAddress, port);
            clientConnectionContainer.Initialize();
            return clientConnectionContainer;
        }

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(string ipAddress, int port, int keySize = 2048) =>
            CreateSecureClientConnectionContainer(ipAddress, port, RSAKeyGeneration.Generate(keySize));

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048) =>
            CreateSecureClientConnectionContainer(ipAddress, port, new RSAPair(publicKey, privateKey, keySize));

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> and connects it to the given IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(string ipAddress, int port, RSAPair rsaPair)
        {
            var secureClientConnectionContainer = new SecureClientConnectionContainer(ipAddress, port, rsaPair);
            secureClientConnectionContainer.Initialize();
            return secureClientConnectionContainer;
        }

        /// <summary>
        /// Creates a <see cref="ClientConnectionContainer"/> with the given <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> to use.</param>
        /// <param name="udpConnection">The <see cref="UdpConnection"/> to use.</param>
        /// <returns>The created <see cref="ClientConnectionContainer"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpConnection"/> is not connected.</exception>
        public static ClientConnectionContainer CreateClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection)
        {
            if (tcpConnection == null || !tcpConnection.IsAlive)
                throw new ArgumentException("TCP connection must be connected to an endpoint.");

            var clientConnectionContainer = new ClientConnectionContainer(tcpConnection, udpConnection);
            clientConnectionContainer.Initialize();
            return clientConnectionContainer;
        }

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> with the given <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> to use.</param>
        /// <param name="udpConnection">The <see cref="UdpConnection"/> to use.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpConnection"/> is not connected.</exception>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection, int keySize = 2048) =>
            CreateSecureClientConnectionContainer(tcpConnection, udpConnection, RSAKeyGeneration.Generate(keySize));

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> with the given <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> to use.</param>
        /// <param name="udpConnection">The <see cref="UdpConnection"/> to use.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpConnection"/> is not connected.</exception>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection,
            string publicKey, string privateKey, int keySize = 2048) => CreateSecureClientConnectionContainer(tcpConnection, udpConnection, new RSAPair(publicKey, privateKey, keySize));

        /// <summary>
        /// Creates a <see cref="SecureClientConnectionContainer"/> with the given <see cref="TcpConnection"/> and <see cref="UdpConnection"/>.
        /// </summary>
        /// <param name="tcpConnection">The <see cref="TcpConnection"/> to use.</param>
        /// <param name="udpConnection">The <see cref="UdpConnection"/> to use.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <returns>The created <see cref="SecureClientConnectionContainer"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the given <see cref="TcpConnection"/> is not connected.</exception>
        public static ClientConnectionContainer CreateSecureClientConnectionContainer(TcpConnection tcpConnection, UdpConnection udpConnection, RSAPair rsaPair)
        {
            if (tcpConnection == null || !tcpConnection.IsAlive)
                throw new ArgumentException("TCP connection must be connected to an endpoint.");

            var secureClientConnectionContainer = new SecureClientConnectionContainer(tcpConnection, udpConnection, rsaPair);
            secureClientConnectionContainer.Initialize();
            return secureClientConnectionContainer;
        }

        #endregion Client Connection Container Factory

        #region Server Connection Container Factory

        /// <summary>
        /// Creates a <see cref="ServerConnectionContainer"/> listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="ServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateServerConnectionContainer(int port, bool start = true) => new ServerConnectionContainer(port, start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(int port, int keySize = 2048, bool start = true) =>
            CreateSecureServerConnectionContainer(port, RSAKeyGeneration.Generate(keySize), start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(int port, string publicKey, string privateKey, int keySize = 2048, bool start = true) =>
            CreateSecureServerConnectionContainer(port, new RSAPair(publicKey, privateKey, keySize), start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> listening on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(int port, RSAPair rsaPair, bool start = true) => new SecureServerConnectionContainer(port, rsaPair, start);

        /// <summary>
        /// Creates a <see cref="ServerConnectionContainer"/> with the given IP address, listening on the given port.
        /// </summary>
        /// <param name="ipAddress">The IP address to run at.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="ServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateServerConnectionContainer(string ipAddress, int port, bool start = true) => new ServerConnectionContainer(ipAddress, port, start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> with the given IP address, listening on the given port.
        /// </summary>
        /// <param name="ipAddress">The IP address to run at.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(string ipAddress, int port, int keySize = 2048, bool start = true) =>
            CreateSecureServerConnectionContainer(ipAddress, port, RSAKeyGeneration.Generate(keySize), start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> with the given IP address, listening on the given port.
        /// </summary>
        /// <param name="ipAddress">The IP address to run at.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="publicKey">The public RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="privateKey">The private RSA key in xml format. (https://superdry.apphb.com/tools/online-rsa-key-converter)</param>
        /// <param name="keySize">The size of the RSA keys.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(string ipAddress, int port, string publicKey, string privateKey, int keySize = 2048, bool start = true) =>
            CreateSecureServerConnectionContainer(ipAddress, port, new RSAPair(publicKey, privateKey, keySize), start);

        /// <summary>
        /// Creates a <see cref="SecureServerConnectionContainer"/> with the given IP address, listening on the given port.
        /// </summary>
        /// <param name="ipAddress">The IP address to run at.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="rsaPair">The RSA key-pair to use.</param>
        /// <param name="start">Whether to start the server after instantiation.</param>
        /// <returns>The created <see cref="SecureServerConnectionContainer"/>.</returns>
        public static ServerConnectionContainer CreateSecureServerConnectionContainer(string ipAddress, int port, RSAPair rsaPair, bool start = true) =>
            new SecureServerConnectionContainer(ipAddress, port, rsaPair);

        #endregion Server Connection Container Factory

        #endregion Methods
    }
}