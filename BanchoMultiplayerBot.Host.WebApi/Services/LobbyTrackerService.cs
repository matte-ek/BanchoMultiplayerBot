using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Hubs;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

/// <summary>
/// This service will track events for all lobbies created by the bot, such as when people join or leave, messages and more.
/// We do this to provide realtime events to the frontend.
/// </summary>
public class LobbyTrackerService(Bot bot, IServiceScopeFactory serviceScopeFactory)
{
    private readonly List<LobbyInstance> _lobbyInstances = [];
    
    /// <summary>
    /// Start listening to new lobbies being created or removed
    /// </summary>
    public void Start()
    {
        bot.OnLobbyCreated += OnLobbyCreated;   
        bot.OnLobbyRemoved += OnLobbyRemoved;   
        
        bot.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;
        bot.BanchoConnection.MessageHandler.OnMessageSent += OnMessageSent;
    }
    
    /// <summary>
    /// Stop listening to new lobbies
    /// </summary>
    public void Stop()
    {
        bot.OnLobbyCreated -= OnLobbyCreated;
        bot.OnLobbyRemoved -= OnLobbyRemoved;   
        
        bot.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;
        bot.BanchoConnection.MessageHandler.OnMessageSent -= OnMessageSent;
        
        _lobbyInstances.Clear();
    }
    
    /// <summary>
    /// Find tracked data about a lobby instance
    /// </summary>
    public LobbyInstance? GetLobbyInstance(int lobbyId)
    {
        return _lobbyInstances.FirstOrDefault(x => x.Lobby.LobbyConfigurationId == lobbyId);
    }
    
    private void OnLobbyCreated(ILobby lobby)
    {
        var instance = new LobbyInstance(lobby, serviceScopeFactory);
        
        lobby.OnStarted += instance.OnStarted;
        lobby.OnStopped += instance.OnStopped;
        
        _lobbyInstances.Add(instance);   
    }
    
    private void OnLobbyRemoved(ILobby lobby)
    {
        var instance = new LobbyInstance(lobby, serviceScopeFactory);

        lobby.OnStarted -= instance.OnStarted;
        lobby.OnStopped -= instance.OnStopped;
        
        _lobbyInstances.Remove(instance);
    }
    
    private void OnMessageSent(IPrivateIrcMessage msg)
    {
        var instance = _lobbyInstances.FirstOrDefault(x => x.Lobby.MultiplayerLobby?.ChannelName == msg.Recipient);

        instance?.AddMessage(new MessageModel
        {
            Author = msg.Sender,
            Content = msg.Content,
            Timestamp = DateTime.UtcNow,
            IsBanchoMessage = false,
            IsAdministratorMessage = true // Since we're in the "sent" event, we know it's an administrator message
        });
    }

    private void OnMessageReceived(IPrivateIrcMessage msg)
    {
        var instance = _lobbyInstances.FirstOrDefault(x => x.Lobby.MultiplayerLobby?.ChannelName == msg.Recipient);

        instance?.AddMessage(new MessageModel
        {
            Author = msg.Sender,
            Content = msg.Content,
            Timestamp = DateTime.UtcNow,
            IsBanchoMessage = msg.IsBanchoBotMessage,
            IsAdministratorMessage = false
        });
    }
    
    public class LobbyInstance(ILobby lobby, IServiceScopeFactory serviceScopeFactory)
    {
        public ILobby Lobby { get; } = lobby;
        
        /// <summary>
        /// A list of the previous 500 messages in the lobby
        /// </summary>
        public List<MessageModel> Messages { get; } = [];

        /// <summary>
        /// Incrementing message id, to keep track of a unique id for each message
        /// </summary>
        private int _messageId;

        /// <summary>
        /// This is called whenever the lobby instance for a channel is built
        /// </summary>
        public void OnStarted()
        {
            if (Lobby.MultiplayerLobby == null)
            {
                Log.Error("{Component}: MultiplayerLobby is null for {LobbyId}", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
                return;
            }
            
            if (Lobby.BehaviorEventProcessor == null)
            {
                Log.Error("{Component}: BehaviorEventProcessor is null for {LobbyId}", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
                return;
            }
            
            Lobby.MultiplayerLobby.OnPlayerJoined += OnPlayerJoined;
            Lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
            Lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
            Lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
            Lobby.MultiplayerLobby.OnMatchAborted += OnMatchAborted;
            Lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
            Lobby.BehaviorEventProcessor.OnExternalBehaviorEvent += OnBehaviorEvent;
            
            Log.Information("{Component}: Started tracking lobby {LobbyId}!", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
        }

        /// <summary>
        /// This is called whenever the lobby instance for a channel is disposed
        /// </summary>
        public void OnStopped()
        {
            if (Lobby.MultiplayerLobby == null)
            {
                Log.Error("{Component}: MultiplayerLobby is null for {LobbyId}", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
                return;
            }
            
            if (Lobby.BehaviorEventProcessor == null)
            {
                Log.Error("{Component}: BehaviorEventProcessor is null for {LobbyId}", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
                return;
            }
            
            Lobby.MultiplayerLobby.OnPlayerJoined -= OnPlayerJoined;
            Lobby.MultiplayerLobby.OnPlayerDisconnected -= OnPlayerDisconnected;
            Lobby.MultiplayerLobby.OnMatchStarted -= OnMatchStarted;
            Lobby.MultiplayerLobby.OnMatchFinished -= OnMatchFinished;
            Lobby.MultiplayerLobby.OnMatchAborted -= OnMatchAborted;
            Lobby.MultiplayerLobby.OnSettingsUpdated -= OnSettingsUpdated;
            Lobby.BehaviorEventProcessor.OnExternalBehaviorEvent -= OnBehaviorEvent;
            
            Log.Information("{Component}: Stopped tracking lobby {LobbyId}!", nameof(LobbyTrackerService), Lobby.LobbyConfigurationId);
        }

        public void AddMessage(MessageModel messageModel)
        {
            messageModel.Id = _messageId++;
            
            Messages.Add(messageModel);

            if (Messages.Count > 500)
            {
                Messages.RemoveAt(0);
            }
            
            BroadcastEvent("onMessage", messageModel);
        }
        
        private void OnSettingsUpdated()
        {
            BroadcastEvent("onSettingsUpdated");
        }
        
        private void OnPlayerJoined(MultiplayerPlayer player)
        {
            BroadcastEvent("onPlayerJoined", new PlayerModel
            {
                OsuId = player.Id,
                Name= player.Name
            });
        }
        
        private void OnPlayerDisconnected(PlayerDisconnectedEventArgs player)
        {
            BroadcastEvent("onPlayerDisconnected", new PlayerModel
            {
                OsuId = player.Player.Id,
                Name= player.Player.Name
            });
        }
        
        private void OnBehaviorEvent(string eventName)
        {
            if (eventName == "MapManagerNewMap")
            {
                BroadcastEvent("onBeatmapChanged");
            }
        }
        
        private void OnMatchStarted() => BroadcastEvent("onMatchStarted");
        
        private void OnMatchFinished() => BroadcastEvent("onMatchFinished");
        
        private void OnMatchAborted() => BroadcastEvent("onMatchAborted");

        /// <summary>
        /// Sends an arbitrary event to all listening SignalR clients, non-blocking
        /// </summary>
        private void BroadcastEvent(string name, object? data = null)
        {
            Task.Run(() =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                
                var hub = scope.ServiceProvider.GetRequiredService<IHubContext<LobbyEventHub>>();
                
                hub.Clients.All.SendAsync(name, Lobby.LobbyConfigurationId, data);
            });
        }
    }
}