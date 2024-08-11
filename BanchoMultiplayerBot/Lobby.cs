using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Providers;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BanchoMultiplayerBot
{
    public class Lobby : ILobby
    {
        /// <summary>
        /// The main bot instance
        /// </summary>
        public Bot Bot { get; init; }
        
        /// <summary>
        /// The bancho connection instance
        /// </summary>
        public BanchoConnection BanchoConnection { get; init; }

        /// <summary>
        /// BanchoSharp multiplayer lobby instance, will be null until channel is established
        /// </summary>
        public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;

        /// <summary>
        /// Database id of the lobby configuration
        /// </summary>
        public int LobbyConfigurationId { get; set; }

        /// <summary>
        /// Whether the lobby is ready
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// Event dispatcher for behavior events
        /// </summary>
        public IBehaviorEventProcessor? BehaviorEventProcessor { get; private set; }
        
        public ITimerProvider? TimerProvider { get; private set; }
        
        public IVoteProvider? VoteProvider { get; private set; }
        
        private string _channelId = string.Empty;
        
        private bool _isCreatingInstance;

        public Lobby(Bot bot, int lobbyConfigurationId)
        {
            Bot = bot;
            BanchoConnection = bot.BanchoConnection;

            BanchoConnection.ChannelHandler.OnChannelJoined += OnChannelJoined;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure += OnChannelJoinedFailure;
            BanchoConnection.ChannelHandler.OnLobbyCreated += OnLobbyCreated;

            LobbyConfigurationId = lobbyConfigurationId;
        }

        public async Task Dispose()
        {
            BanchoConnection.ChannelHandler.OnChannelJoined -= OnChannelJoined;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure -= OnChannelJoinedFailure;
            BanchoConnection.ChannelHandler.OnLobbyCreated -= OnLobbyCreated;

            await ShutdownInstance();
        }

        /// <summary>
        /// Attempt to the lobby to a bancho channel, if the channel does not exist, a new one will be created.
        /// Requires a bancho connection to be established.
        /// </summary>
        public async Task ConnectAsync()
        {
            IsReady = false;
            
            if (BanchoConnection.BanchoClient == null)
            {
                throw new InvalidOperationException("BanchoClient is not initialized during lobby connection attempt.");
            }

            if (MultiplayerLobby != null)
            {
                Log.Verbose("Lobby: Lobby instance already exists, disposing of previous instance...");
                await ShutdownInstance();
            }
            
            var lobbyConfiguration = await GetLobbyConfiguration();
            var previousInstance = await GetRecentRoomInstance();
            var existingChannel = string.Empty;
            
            // If we have a previous instance, attempt to join via that channel instead.
            if (previousInstance != null)
            {
                existingChannel = previousInstance.Channel;
            }
            
            _channelId = existingChannel;
            _isCreatingInstance = existingChannel.Length == 0;
            
            if (!_isCreatingInstance)
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

        public async Task<LobbyConfiguration> GetLobbyConfiguration()
        {
            await using var context = new BotDbContext();

            var configuration = await context.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == LobbyConfigurationId);
            if (configuration == null)
            {
                Log.Error("Lobby: Failed to find lobby configuration.");

                throw new InvalidOperationException("Failed to find lobby configuration.");
            }

            return configuration;
        }

        private async Task BuildInstance()
        {
            var lobbyConfiguration = await GetLobbyConfiguration();

            BehaviorEventProcessor = new BehaviorEventProcessor(this);
            TimerProvider = new TimerProvider(this);
            VoteProvider = new VoteProvider(this);
            
            // Load the default behaviors
            BehaviorEventProcessor.RegisterBehavior("RoomManagerBehavior");
            BehaviorEventProcessor.RegisterBehavior("AbortVoteBehavior");
            BehaviorEventProcessor.RegisterBehavior("AntiAfkBehavior");
            BehaviorEventProcessor.RegisterBehavior("AutoStartBehavior");
            BehaviorEventProcessor.RegisterBehavior("HostQueueBehavior");
            BehaviorEventProcessor.RegisterBehavior("MapManagerBehavior");
            BehaviorEventProcessor.RegisterBehavior("FunCommandsBehavior");
    
            // Load custom behaviors
            if (lobbyConfiguration.Behaviours != null)
            {
                foreach (var behavior in lobbyConfiguration.Behaviours)
                {
                    BehaviorEventProcessor.RegisterBehavior(behavior);
                }
            }

            BehaviorEventProcessor.Start();

            await TimerProvider.Start();
            await VoteProvider.Start();
            
            // Make sure we have a database entry for this lobby instance
            var recentRoomInstance = await GetRecentRoomInstance(_channelId);
            if (recentRoomInstance == null)
            {
                await using var context = new BotDbContext();

                var newInstance = new LobbyRoomInstance()
                {
                    Channel = _channelId,
                    LobbyConfigurationId = LobbyConfigurationId
                };
                
                context.LobbyRoomInstances.Add(newInstance);
                
                await context.SaveChangesAsync();
            }

            IsReady = true;
            
            Log.Verbose("Lobby: Lobby instance built successfully");
        }

        private async Task ShutdownInstance()
        {
            if (TimerProvider != null)
            {
                await TimerProvider.Stop();
                TimerProvider = null;
            }
            
            if (VoteProvider != null)
            {
                await VoteProvider.Stop();
                VoteProvider = null;
            }

            BehaviorEventProcessor?.Stop();
            BehaviorEventProcessor = null;

            MultiplayerLobby = null;
            
            Log.Verbose("Lobby: Lobby instance shutdown successfully");
        }
        
        private async void OnLobbyCreated(IMultiplayerLobby lobby)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning("Lobby: BanchoConnection.BanchoClient is null during lobby creation event");
                return;
            }

            if (!_isCreatingInstance)
            {
                // This event isn't for us.
                return;
            }

            _channelId = lobby.ChannelName;
            
            // I don't think there should be any issues using the multiplayer lobby provided from the event,
            // but we'll create our own anyway.
            MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(lobby.ChannelName[4..]), lobby.ChannelName);

            await BuildInstance();
        }

        private async void OnChannelJoined(IChatChannel channel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning("Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
                return;
            }

            if (channel.ChannelName != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }

            if (_isCreatingInstance)
            {
                // We will be waiting for the lobby creation event instead
                return;
            }
            
            Log.Verbose("Lobby: Joined channel {Channel} successfully, building lobby instance...", channel.ChannelName);

            MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(channel.ChannelName[4..]),
                channel.ChannelName);

            await BuildInstance();
        }

        private async void OnChannelJoinedFailure(string attemptedChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning("Lobby: BanchoConnection.BanchoClient is null during channel join failure.");
                return;
            }

            if (attemptedChannel != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }

            var lobbyConfiguration = await GetLobbyConfiguration();

            Log.Warning("Lobby: Failed to join channel {AttemptedChannel}, creating new lobby instead.",
                attemptedChannel);

            await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name)!;
        }

        private async Task<LobbyRoomInstance?> GetRecentRoomInstance(string? channelId = null)
        {
            await using var context = new BotDbContext();

            var query = context.LobbyRoomInstances
                .OrderByDescending(x => x.Id)
                .Where(x => x.LobbyConfigurationId == LobbyConfigurationId);
            
            if (channelId != null)
            {
                query = query.Where(x => x.Channel == channelId);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}