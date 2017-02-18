using Network;
using Network.Bluetooth;
using System;
using TestServerClientPackets;

namespace NetworkTestClient
{
    /// <summary>
    /// Simple example>
    //1. Get the clients in range.
    //2. Create a new instance of the bluetoothConnection with the factory.
    //3. Register what happens if we receive a packet of type "CalculationResponse"
    //4. Send a calculation request
    /// </summary>
    public class BluetoothExample
    {
        public async void Demo()
        {
            if(!BluetoothConnection.IsBluetoothSupported)
            {
                Console.WriteLine("Bluetooth is not supported on this Device");
                return;
            }

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
    }
}