using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp;
using Prometheus;
using Serilog;
using System.Threading;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho
{
    public class BanchoConnection : IBanchoConnection
    {
        public bool IsConnected { get; set; } = false;

        public IBanchoClient? BanchoClient { get; private set; } = null!;

        public IMessageHandler MessageHandler { get; }
        public ICommandHandler CommandHandler { get; }
        public IChannelHandler ChannelHandler { get; }
        
        public event Action? OnReady;

        public CancellationToken? ConnectionCancellationToken => _cancellationTokenSource?.Token;

        private readonly BanchoClientConfiguration _banchoConfiguration;
        private ConnectionHandler? _connectionWatchdog;
        private CancellationTokenSource? _cancellationTokenSource;

        private static readonly Gauge ConnectionHealth = Metrics.CreateGauge("bot_connection_health", "Bancho connection health");

        public BanchoConnection(BanchoClientConfiguration banchoClientConfiguration) 
        {
            _banchoConfiguration = banchoClientConfiguration;

            MessageHandler = new MessageHandler(this);
            ChannelHandler = new ChannelHandler(this);
            CommandHandler = new CommandHandler(MessageHandler);
        }

        public Task StartAsync()
        {
            _ = Task.Run(ConnectAsync);

            return Task.CompletedTask;
        }


        public async Task StopAsync()
        {
            await DisconnectAsync();
        }

        private async Task ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Make sure we've fully disconnected before continuing,
            // we more or less want a fully reset state.
            await DisconnectAsync();

            BanchoClient = new BanchoClient(
                                    new BanchoClientConfig(new IrcCredentials(_banchoConfiguration.Username, _banchoConfiguration.Password), 
                                    LogLevel.None,
                                    false));

            SubscribeEvents();
            
            Log.Information("BanchoConnection: Connecting to Bancho...");

            try
            {
                await BanchoClient.ConnectAsync();
            }
            catch (Exception e)
            {
                Log.Error("BanchoConnection: Exception during connection to Bancho, {Exception}", e);
                return;
            }

            Log.Information("BanchoConnection: Bancho connection terminated");
        }

        private async Task DisconnectAsync()
        {
            ConnectionHealth.Set(0);

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

            ChannelHandler.Stop();

            if (BanchoClient != null)
            {
                Log.Information("BanchoConnection: Disconnecting from Bancho...");
                
                await BanchoClient.DisconnectAsync();

                UnsubscribeEvents();

                BanchoClient?.Dispose();
            }

            BanchoClient = null;
            IsConnected = false;
        }

        private void BanchoOnAuthenticated()
        {
            if (BanchoClient?.TcpClient == null)
            {
                // Shouldn't ever happen, hopefully.
                return;
            }

            Log.Information("BanchoConnection: Authenticated with Bancho successfully");
            
            IsConnected = true;

            ConnectionHealth.Set(1);

            // Once we got a connection successfully up and running, make sure to initiate
            // the connection watchdog immediately
            _connectionWatchdog = new ConnectionHandler(BanchoClient.TcpClient, MessageHandler);
            _connectionWatchdog.OnConnectionLost += OnConnectionLost;
            _connectionWatchdog.Start();

            ChannelHandler.Start();
            MessageHandler.Start();
            
            OnReady?.Invoke();
        }

        private void OnConnectionLost()
        {
            IsConnected = false;
            _cancellationTokenSource?.Cancel();
            ConnectionHealth.Set(0);

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
