# C# Network Library

[![forthebadge](https://forthebadge.com/images/badges/as-seen-on-tv.svg)](https://forthebadge.com)
[![forthebadge](https://forthebadge.com/images/badges/gluten-free.svg)](https://forthebadge.com)

Tutorials http://www.indie-dev.at/?cat=86 <br />
Downloads http://www.indie-dev.at/?page_id=480 <br />
Licence http://www.indie-dev.at/?page_id=525 <br />
Forum http://www.indie-dev.at/?forum=c-network-library <br />
Documentation http://www.indie-dev.at/?page_id=476 <br />
NuGet https://www.nuget.org/packages/Network/ <br />

# Continuous Integration

| Build Configuration | Framework | Badge |
| ------------------- | --------- | ----- |
| Debug | .NET Framework | [![Build status](https://thomaschristof.visualstudio.com/Network/_apis/build/status/DEBUG%20.NET%20Desktop)](https://thomaschristof.visualstudio.com/Network/_build/latest?definitionId=-1) |
| Release | .NET Framework | [![Build Status](https://thomaschristof.visualstudio.com/Network/_apis/build/status/RELEASE%20.NET%20Framework)](https://thomaschristof.visualstudio.com/Network/_build/latest?definitionId=11) |
| Debug | .NET Core | [![Build status](https://thomaschristof.visualstudio.com/Network/_apis/build/status/DEBUG%20.NET%20Core)](https://thomaschristof.visualstudio.com/Network/_build/latest?definitionId=-1) |
| Release | .NET Core | [![Build status](https://thomaschristof.visualstudio.com/Network/_apis/build/status/RELEASE%20.NET%20Core)](https://thomaschristof.visualstudio.com/Network/_build/latest?definitionId=9) |

# Badges

| Service | Description | Badge |
| ------- | ----------- | ----- |
| CodeFactor | Code Quality | [![CodeFactor](https://www.codefactor.io/repository/github/toemsel/network/badge)](https://www.codefactor.io/repository/github/toemsel/network) |
| NuGet | Download Count | ![NuGet](https://img.shields.io/nuget/dt/Network.svg) |
| NuGet | Current Version | ![NuGet](https://img.shields.io/nuget/v/Network.svg) |
| Discord | Chat-Room | ![Discord](https://img.shields.io/discord/502989126916898837.svg) |


# Supported Frameworks

- .NET Framework          >= 4.6
- .NET Core*               >= 2.0
- Mono*                    >= 5.4
- Xamarin.iOS*             >= 10.14
- Xamarin.MAC*             >= 3.8
- Xamarin.Android*         >= 8.0
- UWP*                     >= 10.0.16299

'*' No Bluetooth support

# Features in a nutshell

- TCP communication
- UDP communication
- Bluetooth communication
- Factories to ensure the most easy setup
- Server and Client Wrappers (Auto-Reconnect, Auto-Join)
- Object oriented. Don't worry about bits and bytes. **Send and receive objects**
- Optional RSA encryption for TCP and UDP
- Use lambdas, delegates or even async operations to send and receive objects
- Useful helpers to quickly send small information, without creating an object
- Logging for debugging or traffic inspection
- No magic numbers, identifiers or configurations required
- Very fast and relieable (6-10ms RTT)
- Highly customizable
- OpenSource and Free to use

# Donations
- LTC: LYSaNyRArm1jQdAxYXf7GDFSCuoGnVSVSf
- TRX: TVUiQD8LpKScXv2TMxYb7c2bKA2Xdn8ayn
- ETH: 0xa1249ae140e775d75dd1e96b366cf605264c3f46
- BTC: 15C8ats3mqKB9F86cZVcL6BcFGNfTLDpM6
- XRP: rEb8TK3gBgk5auZkwc6sHnwrGVJH8DuaLh

# Example Client
```c#
        public void Demo()
        {
            ConnectionResult connectionResult = ConnectionResult.TCPConnectionNotAlive;
            //1. Establish a connection to the server.
            TcpConnection tcpConnection = ConnectionFactory.CreateTcpConnection("127.0.0.1", 1234, out connectionResult);
            //2. Register what happens if we get a connection
            if(connectionResult == ConnectionResult.Connected)
            {
                Console.WriteLine($"{tcpConnection.ToString()} Connection established");
                //3. Send a raw data packet request.
                tcpConnection.SendRawData(RawDataConverter.FromUTF8String("HelloWorld", "Hello, this is the RawDataExample!"));
                tcpConnection.SendRawData(RawDataConverter.FromBoolean("BoolValue", true));
                tcpConnection.SendRawData(RawDataConverter.FromBoolean("BoolValue", false));
                tcpConnection.SendRawData(RawDataConverter.FromDouble("DoubleValue", 32.99311325d));
                //4. Send a raw data packet request without any helper class
                tcpConnection.SendRawData("HelloWorld", Encoding.UTF8.GetBytes("Hello, this is the RawDataExample!"));
                tcpConnection.SendRawData("BoolValue", BitConverter.GetBytes(true));
                tcpConnection.SendRawData("BoolValue", BitConverter.GetBytes(false));
                tcpConnection.SendRawData("DoubleValue", BitConverter.GetBytes(32.99311325d));
            }
        }
```

# Example Server
```c#
        public void Demo()
        {
            //1. Start listen on a port
            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(1234, false);

            //2. Apply optional settings.
            #region Optional settings
            serverConnectionContainer.ConnectionLost += (a, b, c) => Console.WriteLine($"{serverConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            serverConnectionContainer.ConnectionEstablished += connectionEstablished;
            serverConnectionContainer.AllowBluetoothConnections = true;
            serverConnectionContainer.AllowUDPConnections = true;
            serverConnectionContainer.UDPConnectionLimit = 2;
            #endregion Optional settings

            //Call start here, because we had to enable the bluetooth property at first.
            serverConnectionContainer.Start();
        }

        /// <summary>
        /// We got a connection.
        /// </summary>
        /// <param name="connection">The connection we got. (TCP or UDP)</param>
        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{serverConnectionContainer.Count} {connection.GetType()} connected on port {connection.IPRemoteEndPoint.Port}");

            //3. Register packet listeners.
            connection.RegisterRawDataHandler("HelloWorld", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToUTF8String()}"));
            connection.RegisterRawDataHandler("BoolValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToBoolean()}"));
            connection.RegisterRawDataHandler("DoubleValue", (rawData, con) => Console.WriteLine($"RawDataPacket received. Data: {rawData.ToDouble()}"));
        }
```

<img src="http://www.indie-dev.at/wp-content/uploads/2016/11/Demo.gif" />

# Async Example
```c#
        public async void Demo()
        {
            //1. Establish a connection to the server.
            ClientConnectionContainer container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += async (connection, type) =>
            {
                Console.WriteLine($"{type.ToString()} Connection established");
                //3. Send a request packet async and directly receive an answer.
                CalculationResponse response = await connection.SendAsync<CalculationResponse>(new CalculationRequest(10, 10));
                Console.WriteLine($"Answer received {response.Result}");
            };
        }
```

# Delegate Example
```c#
        private ClientConnectionContainer container;

        public void Demo()
        {
            //1. Establish a connection to the server.
            container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += connectionEstablished;
        }

        private void connectionEstablished(Connection connection, ConnectionType type)
        {
            Console.WriteLine($"{type.ToString()} Connection established");
            //3. Register what happens if we receive a packet of type "CalculationResponse"
            container.RegisterPacketHandler<CalculationResponse>(calculationResponseReceived, this);
            //4. Send a calculation request.
            connection.Send(new CalculationRequest(10, 10), this);
        }

        private void calculationResponseReceived(CalculationResponse response, Connection connection)
        {
            //5. CalculationResponse received.
            Console.WriteLine($"Answer received {response.Result}");
        }
 ```
 
# Lambda Example
 ```c#
         public void Demo()
        {
            //1. Establish a connection to the server.
            ClientConnectionContainer container = ConnectionFactory.CreateClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += (connection, type) =>
            {
                Console.WriteLine($"{type.ToString()} Connection established");
                //3. Register what happens if we receive a packet of type "CalculationResponse"
                connection.RegisterPacketHandler<CalculationResponse>((response, con) => Console.WriteLine($"Answer received {response.Result}"), this);
                //4. Send a calculation request.
                connection.Send(new CalculationRequest(10, 10), this);
            };
        }
 ```
 
# Bluetooth Example
  ```c#
          public async void Demo()
        {
            //1. Get the clients in range.
            DeviceInfo[] devicesInRange = await ConnectionFactory.GetBluetoothDevicesAsync();
            if(devicesInRange.Length <= 0) return; //We need at least one bluetooth connection to deal with :)
           //2. Create a new instance of the bluetoothConnection with the factory.
            Tuple<ConnectionResult, BluetoothConnection> bluetoothConnection = await ConnectionFactory.CreateBluetoothConnectionAsync(devicesInRange[0]);
            if(bluetoothConnection.Item1 != ConnectionResult.Connected) return; //We were not able to connect to the server.
            //3. Register what happens if we receive a packet of type "CalculationResponse"
            bluetoothConnection.Item2.RegisterPacketHandler<CalculationResponse>((response, con) => Console.WriteLine($"Answer received {response.Result}"), this);
            //4. Send a calculation request.
            bluetoothConnection.Item2.Send(new CalculationRequest(10, 10), this);
        }
   ```
   
# RSA Example
   ```c#
        public async void Demo()
        {
            //1. Establish a connection.
            ClientConnectionContainer container = ConnectionFactory.CreateSecureClientConnectionContainer("127.0.0.1", 1234);
            //2. Register what happens if we get a connection
            container.ConnectionEstablished += (connection, type) =>
            {
                Console.WriteLine($"{type.ToString()} Connection established");
                //3. Register what happens if we receive a packet of type "CalculationResponse"
                connection.RegisterPacketHandler<CalculationResponse>((response, con) => Console.WriteLine($"Answer received {response.Result}"), this);
                //4. Send a calculation request.
                connection.Send(new CalculationRequest(10, 10), this);
            };
        }
   ```

# Logging

<img src="http://www.indie-dev.at/wp-content/uploads/2016/11/Logging.gif" />

# Logging with RSA

<img src="https://www.indie-dev.at/wp-content/uploads/2018/08/RSA.gif" />
