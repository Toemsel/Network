using Network;
using System.Windows;
using TestServerClientPackets.ExamplePacketsThree;

namespace NetworkReactiveTestClient
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            var connectionResult = await ConnectionFactory.CreateTcpConnectionAsync("127.0.0.1", 1234);
            TcpConnection tcpConnection = connectionResult.Item1;
            tcpConnection.ConnectionEstablished += (connection, type) => MessageBox.Show("Client is connected with the server");
            tcpConnection.UnlockRemoteConnection();

            tcpConnection.RegisterPacketHandler<RandomViewModel>((c, p) =>
            {
                Dispatcher.Invoke(() => DataContext = c);
            }, this);        
        }
    }
}
