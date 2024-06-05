using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp;
using Serilog;

namespace BanchoMultiplayerBot.Bancho
{
    public class BanchoConnection : IBanchoConnection
    {
        public bool IsConnected { get; set; } = false;

        public BanchoClient? BanchoClient { get; private set; } = null!;

        public IMessageHandler MessageHandler { get; } = null!;
        public ICommandHandler CommandHandler { get; } = null!;

        private BanchoClientConfiguration _banchoConfiguration = null!;
        private IConnectionHandler? _connectionWatchdog = null!;

        public BanchoConnection(BanchoClientConfiguration banchoClientConfiguration) 
        {
            _banchoConfiguration = banchoClientConfiguration;

            MessageHandler = new MessageHandler(this);
            CommandHandler = new CommandHandler(MessageHandler);
        }

        public async Task ConnectAsync()
        {
            // Make sure we've fully disconnected before continuing,
            // we more or less want a fully reset state.
            await DisconnectAsync();

            BanchoClient = new BanchoClient(
                                    new BanchoClientConfig(new IrcCredentials(_banchoConfiguration.Username, _banchoConfiguration.Password), 
                                    LogLevel.None,
                                    false));

            SubscribeEvents();

            await BanchoClient.ConnectAsync();
        }

        public async Task DisconnectAsync()
        {
            if (_connectionWatchdog?.IsRunning == true)
            {
                _connectionWatchdog.OnConnectionLost -= OnConnectionLost;
                _connectionWatchdog.Stop();
                _connectionWatchdog = null;
            }

            if (MessageHandler.IsRunning)
            {
                MessageHandler.Stop();
            }

            if (BanchoClient != null)
            {
                await BanchoClient.DisconnectAsync();

                UnsubscribeEvents();

                BanchoClient?.Dispose();
            }

            BanchoClient = null;
            IsConnected = false;
        }

        private void BanchoOnAuthenticated()
        {
            if (BanchoClient == null || BanchoClient?.TcpClient == null)
            {
                // Shouldn't ever happen, hopefully.
                return;
            }

            IsConnected = true;

            // Once we got a connection successfuly up and running, make sure to initiate
            // the connection watchdog immediately
            _connectionWatchdog = new ConnectionHandler(BanchoClient.TcpClient, MessageHandler);
            _connectionWatchdog.OnConnectionLost += OnConnectionLost;
            _connectionWatchdog.Start();

            MessageHandler.Start();
        }

        private void OnConnectionLost()
        {
            IsConnected = false;

            Log.Error("BanchoConnection: Connection lost, attempting to reconnect in 20 seconds...");

            Thread.Sleep(20000);

            int connectionAttempts = 0;
            while (connectionAttempts < 10)
            {
                Log.Information("BanchoConnection: Attempting to reconnect...");

                _ = Task.Run(ConnectAsync);

                Thread.Sleep(10000);

                // If we're back in action, IsConnected will be true
                // we can safely exit due to a new watchdog being started
                // so even if we lose connection again, we'll be able to
                // reconnect.
                if (IsConnected)
                {
                    Log.Information("BanchoConnection: Reconnected successfully");

                    return;
                }

                Log.Error("BanchoConnection: Reconnection failed, retrying in 10 seconds...");

                Thread.Sleep(10000);

                connectionAttempts++;
            }

            Log.Fatal("BanchoConnection: Failed to reconnect after 10 attempts, shutting down...");

            DisconnectAsync().Wait(TimeSpan.FromSeconds(30));
        }

        private void SubscribeEvents()
        {
            if (BanchoClient == null)
            {
                return;
            }

            BanchoClient.OnAuthenticated += BanchoOnAuthenticated;
        }

        private void UnsubscribeEvents()
        {
            if (BanchoClient == null)
            {
                return;
            }

            BanchoClient.OnAuthenticated -= BanchoOnAuthenticated;
        }
    }
}
