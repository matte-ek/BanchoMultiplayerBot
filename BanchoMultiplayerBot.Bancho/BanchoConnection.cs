using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp;
using Prometheus;
using Serilog;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho
{
    /// <summary>
    /// Main class for handling the connection to Bancho within BanchoMultiplayerBot.Bancho.
    /// </summary>
    public class BanchoConnection : IBanchoConnection
    {
        /// <summary>
        /// If the connection to Bancho is currently active.
        /// </summary>
        public bool IsConnected 
        {
            get => _isConnected;
            
            private set
            {
                _isConnected = value;
                ConnectionHealth.Set(_isConnected ? 1 : 0);
            }    
        }

        /// <summary>
        /// The underlying BanchoSharp Bancho client 
        /// </summary>
        public IBanchoClient? BanchoClient { get; private set; }

        /// <summary>
        /// Handler for incoming and outgoing messages to Bancho
        /// </summary>
        public IMessageHandler MessageHandler { get; }
        
        /// <summary>
        /// Handler for executing bancho multiplayer commands, in a more structured way.
        /// </summary>
        public ICommandHandler CommandHandler { get; }
        
        /// <summary>
        /// Handler for managing channels that we are connected to.
        /// </summary>
        public IChannelHandler ChannelHandler { get; }
        
        /// <summary>
        /// Invoked whenever a connection to Bancho is ready.
        /// </summary>
        public event Action? OnReady;
        
        /// <summary>
        /// Invoked whenever a connection to Bancho has an error, however
        /// the bancho connection will attempt to reconnect automatically.
        /// </summary>
        public event Action? OnConnectionError;

        /// <summary>
        /// Used to cancel the connection to Bancho.
        /// </summary>
        public CancellationToken? ConnectionCancellationToken => _cancellationTokenSource?.Token;

        private readonly BanchoClientConfiguration _banchoConfiguration;
        private ConnectionHandler? _connectionWatchdog;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isConnected;

        private static readonly Gauge ConnectionHealth = Metrics.CreateGauge("bot_connection_health", "Bancho connection health");

        public BanchoConnection(BanchoClientConfiguration banchoClientConfiguration) 
        {
            _banchoConfiguration = banchoClientConfiguration;

            MessageHandler = new MessageHandler(this, banchoClientConfiguration);
            ChannelHandler = new ChannelHandler(this);
            CommandHandler = new CommandHandler(MessageHandler, banchoClientConfiguration);
        }

        /// <summary>
        /// Attempt to connect to Bancho, this is non-blocking, and will
        /// return immediately.
        /// </summary>
        public Task StartAsync()
        {
            _ = Task.Run(ConnectAsync);

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Stop the connection to Bancho, this is blocking, and will
        /// return when the connection is fully terminated.
        /// </summary>
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

            // We set up the BanchoClient here without any logging and such
            // since we deal with that ourselves.
            BanchoClient = new BanchoClient(
                                    new BanchoClientConfig(new IrcCredentials(_banchoConfiguration.Username, _banchoConfiguration.Password), 
                                    LogLevel.None,
                                    false));

            BanchoClient.OnAuthenticated += BanchoOnAuthenticated;
            
            try
            {
                Log.Information("BanchoConnection: Connecting to Bancho...");
                
                // This call is blocking and only return once the connection is
                // terminated, so ideally we'll stay here till something goes wrong.
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

                BanchoClient.OnAuthenticated -= BanchoOnAuthenticated;
                BanchoClient.TcpClient?.Dispose();
                BanchoClient?.Dispose();
            }

            BanchoClient = null;
            IsConnected = false;
            
            Log.Information("BanchoConnection: Disconnected from Bancho successfully");
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

            // Once we got a connection successfully up and running, make sure to initiate
            // the connection watchdog immediately
            _connectionWatchdog = new ConnectionHandler(BanchoClient.TcpClient, MessageHandler);
            _connectionWatchdog.OnConnectionLost += OnConnectionLost;
            _connectionWatchdog.Start();

            ChannelHandler.Start();
            MessageHandler.Start();
            
            OnReady?.Invoke();
        }

        private async void OnConnectionLost()
        {
            Log.Error($"BanchoConnection: Connection lost, attempting to reconnect in {_banchoConfiguration.BanchoReconnectDelay} seconds...");
            
            IsConnected = false;
            
            _cancellationTokenSource?.Cancel();
            
            OnConnectionError?.Invoke();
            
            // Wait a bit before attempting to reconnect
            await Task.Delay(_banchoConfiguration.BanchoReconnectDelay * 1000);

            int connectionAttempts = 0;
            while (connectionAttempts < _banchoConfiguration.BanchoReconnectAttempts)
            {
                Log.Information("BanchoConnection: Attempting to reconnect...");

                _ = Task.Run(ConnectAsync);

                // In normal circumstances, we should be able to reconnect within 10 seconds
                await Task.Delay(10000);

                // If we're back in action, IsConnected will be true
                // we can safely exit due to a new watchdog being started
                // so even if we lose connection again, we'll be able to
                // reconnect.
                if (IsConnected)
                {
                    Log.Information("BanchoConnection: Reconnected successfully");

                    return;
                }
                
                Log.Error($"BanchoConnection: Reconnection failed, retrying in {_banchoConfiguration.BanchoReconnectAttemptDelay} seconds...");

                await Task.Delay(_banchoConfiguration.BanchoReconnectAttemptDelay * 1000);

                connectionAttempts++;
            }

            Log.Fatal($"BanchoConnection: Failed to reconnect after {_banchoConfiguration.BanchoReconnectAttempts} attempts, shutting down...");

            DisconnectAsync().Wait(TimeSpan.FromSeconds(30));
        }
    }
}
