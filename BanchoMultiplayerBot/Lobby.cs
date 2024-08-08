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

        public Lobby(BanchoConnection banchoConnection, int lobbyConfigurationId)
        {
            BanchoConnection = banchoConnection;

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
                Log.Verbose("Lobby ({LobbyConfigId}): Lobby instance already exists, disposing of previous instance...", LobbyConfigurationId);
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
                Log.Verbose("Lobby ({LobbyConfigId}): Attempting to join existing channel '{ExistingChannel}' for lobby '{LobbyName}'...",
                    LobbyConfigurationId,
                    existingChannel,
                    lobbyConfiguration.Name);

                await BanchoConnection.BanchoClient?.SendAsync($"JOIN {existingChannel}")!;
            }
            else
            {
                Log.Verbose("Lobby ({LobbyConfigId}): Creating new channel for lobby '{LobbyName}'", LobbyConfigurationId, lobbyConfiguration.Name);
                
                await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(lobbyConfiguration.Name)!;
            }
        }

        public async Task<LobbyConfiguration> GetLobbyConfiguration()
        {
            await using var context = new BotDbContext();

            var configuration = await context.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == LobbyConfigurationId);
            if (configuration == null)
            {
                Log.Error("Lobby ({LobbyConfigId}): Failed to find lobby configuration.", LobbyConfigurationId);

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
            BehaviorEventProcessor.RegisterBehavior("HostQueueBehavior");
    
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
            
            Log.Verbose("Lobby ({LobbyConfigId}): Lobby instance built successfully", LobbyConfigurationId);
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
            
            Log.Verbose("Lobby ({LobbyConfigId}): Lobby instance shutdown successfully", LobbyConfigurationId);
        }
        
        private async void OnLobbyCreated(IMultiplayerLobby lobby)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning("Lobby ({LobbyConfigId}): BanchoConnection.BanchoClient is null during lobby creation event", LobbyConfigurationId);
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
                Log.Warning("Lobby ({LobbyConfigId}): BanchoConnection.BanchoClient is null during channel join failure.", LobbyConfigurationId);
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
            
            Log.Verbose("Lobby ({LobbyConfigId}): Joined channel {Channel} successfully, building lobby instance...",
                LobbyConfigurationId,
                channel.ChannelName);

            MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(channel.ChannelName[4..]),
                channel.ChannelName);

            await BuildInstance();
        }

        private async void OnChannelJoinedFailure(string attemptedChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                Log.Warning("Lobby({LobbyConfigId}): BanchoConnection.BanchoClient is null during channel join failure.", LobbyConfigurationId);
                return;
            }

            if (attemptedChannel != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }

            var lobbyConfiguration = await GetLobbyConfiguration();

            Log.Warning("Lobby ({LobbyConfigId}): Failed to join channel {AttemptedChannel}, creating new lobby instead.",
                LobbyConfigurationId,
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