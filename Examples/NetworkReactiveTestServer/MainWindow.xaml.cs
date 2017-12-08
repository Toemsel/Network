using Network;
using Network.Enums;
using System.Diagnostics;
using System.Windows;
using TestServerClientPackets.ExamplePacketsThree;

namespace NetworkReactiveTestServer
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServerConnectionContainer serverConnectionContainer;

        public MainWindow()
        {
            InitializeComponent();

            //1. Start listen on a port
            serverConnectionContainer = ConnectionFactory.CreateServerConnectionContainer(1234, false);

            //2. Apply optional settings.
            #region Optional settings
            serverConnectionContainer.ConnectionLost += (a, b, c) => Debug.WriteLine($"{serverConnectionContainer.Count} {b.ToString()} Connection lost {a.IPRemoteEndPoint.Port}. Reason {c.ToString()}");
            serverConnectionContainer.ConnectionEstablished += connectionEstablished;
            serverConnectionContainer.AllowBluetoothConnections = false;
            serverConnectionContainer.AllowUDPConnections = false;
            #endregion Optional settings

            //Call start here, because we had to enable the bluetooth property at first.
            serverConnectionContainer.Start();
        }

        private void connectionEstablished(Connection connection, ConnectionType connectionType)
        {
            connection.EnableLogging = false;

            if (DataContext == null)
            {
                RandomViewModel randomViewModel = new RandomViewModel(1);
                randomViewModel.SyncDirection = SyncDirection.OneWay;
                randomViewModel.AddSyncConnection(connection);
                DataContext = randomViewModel;
                return;
            }

            ((RandomViewModel)DataContext).AddSyncConnection(connection);
        }
    }
}
