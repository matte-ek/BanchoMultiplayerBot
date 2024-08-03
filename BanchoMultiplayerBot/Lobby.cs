using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using System.Text.Json.Nodes;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot
{
    public class Lobby : ILobby
    {
        public BanchoConnection BanchoConnection { get; init; }
        
        /// <summary>
        /// BanchoSharp multiplayer lobby instance, will be null until channel is established
        /// </summary>
        public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;

        /// <summary>
        /// Database id of the lobby configuration
        /// </summary>
        public int LobbyConfigurationId { get; set; } = new();
        
        /// <summary>
        /// Event dispatcher for behavior events
        /// </summary>
        public IBehaviorEventDispatcher? BehaviorEventDispatcher { get; private set; }
        
        /// <summary>
        /// A list of behavior class names to be loaded into the lobby instance
        /// </summary>
        private readonly List<string> _behaviors = [];

        private string _channelId = string.Empty;
        
        private TimerProvider? _timerProvider;
        
        public Lobby(BanchoConnection banchoConnection)
        {
            BanchoConnection = banchoConnection;
            
            BanchoConnection.ChannelHandler.OnChannelJoined += OnChannelJoined;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure += OnChannelJoinedFailure;
        }

        public async Task Dispose()
        {
            BanchoConnection.ChannelHandler.OnChannelJoined -= OnChannelJoined;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure -= OnChannelJoinedFailure;
            
            await ShutdownInstance();
        }
        
        /// <summary>
        /// Attempt to the lobby to a bancho channel, if the channel does not exist, a new one will be created.
        /// Requires a bancho connection to be established.
        /// </summary>
        /// <param name="existingChannel">Optional existing channel, may be invalid.</param>
        public async Task ConnectAsync(string? existingChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                throw new InvalidOperationException("BanchoClient is not initialized during lobby connection attempt.");
            }

            var lobbyConfiguration = await GetLobbyConfiguration();

            _channelId = existingChannel ?? string.Empty;

            if (existingChannel != null)
            {
                Log.Verbose("Lobby: Attempting to join existing channel '{ExistingChannel}' for lobby '{LobbyName}'...", 
                    existingChannel,
                    lobbyConfiguration.Name);

                await BanchoConnection.BanchoClient?.SendAsync($"JOIN {existingChannel}")!;
            }
            else
            {
                Log.Verbose("Lobby: Creating new channel for lobby '{LobbyName}'", lobbyConfiguration.Name);

                await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name)!;
            }
        }
        
        private async Task BuildInstance()
        {
            var lobbyConfiguration = GetLobbyConfiguration();

            BehaviorEventDispatcher = new BehaviorEventDispatcher(this);
            _timerProvider = new TimerProvider(this);
    
            BehaviorEventDispatcher.RegisterBehavior("HostQueueBehavior");
            
            BehaviorEventDispatcher.Start();
            await _timerProvider.Start();
        }

        private async Task ShutdownInstance()
        {
            if (_timerProvider != null)
            {
                await _timerProvider.Stop();
                _timerProvider = null;
            }
            
            BehaviorEventDispatcher?.Stop();
            BehaviorEventDispatcher = null;
        }

        private async void OnChannelJoined(IChatChannel channel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning($"Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
                return;
            }

            if (channel.ChannelName != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }

            Log.Verbose("Lobby: Joined channel {Channel} successfully, building lobby instance...", channel.ChannelName);

            MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(channel.ChannelName[4..]), channel.ChannelName);
            
            await BuildInstance();
        }

        private async void OnChannelJoinedFailure(string attemptedChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning($"Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
                return;
            }
            
            if (attemptedChannel != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }

            var lobbyConfiguration = await GetLobbyConfiguration();

            Log.Warning("Lobby: Failed to join channel {AttemptedChannel}, creating new lobby instead.", attemptedChannel);

            await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name)!;
        }

        private async Task<LobbyConfiguration> GetLobbyConfiguration()
        {
            // FUTURE ME: Throw if lobby configuration is null!
            // Also log since the caller will not expect the call to throw sometimes

            throw new NotImplementedException();
            
            using var context = new BotDbContext();
        } 
    }
}
