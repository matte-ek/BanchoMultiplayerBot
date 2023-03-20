using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Config;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot;

public class Lobby
{
    /// <summary>
    /// Parent bot instance this lobby is running in
    /// </summary>
    public Bot Bot { get; }

    /// <summary>
    /// The BanchoSharp MultiplayerLobby instance running this lobby
    /// </summary>
    public MultiplayerLobby MultiplayerLobby { get; }

    /// <summary>
    /// Reference to the lobby configuration within Bot.Configuration.LobbyConfigurations
    /// </summary>
    public LobbyConfiguration Configuration { get; }
    
    public List<IBotBehaviour> Behaviours { get; } = new();
    
    /// <summary>
    /// If the lobby is recovering existing lobbies, after for example a restart or network connection issue.
    /// </summary>
    public bool IsRecovering { get; private set; }

    /// <summary>
    /// List of the 300 recent messages in the lobby, primarily used within the WebUI
    /// </summary>
    public List<IPrivateIrcMessage> RecentMessages { get; } = new();
    
    public int GamesPlayed { get; private set; }

    public event Action? OnLobbyChannelJoined;
    public event Action<IPrivateIrcMessage>? OnUserMessage;
    public event Action<IPrivateIrcMessage>? OnAdminMessage;
    public event Action<IPrivateIrcMessage>? OnBanchoMessage;
    
    private readonly string _channelName;
    
    public Lobby(Bot bot, LobbyConfiguration configuration, string channel)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = new MultiplayerLobby(Bot.Client, long.Parse(channel[4..]), channel);
        _channelName = channel;
    }
    
    public Lobby(Bot bot, LobbyConfiguration configuration, MultiplayerLobby lobby)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = lobby;
        _channelName = lobby.ChannelName;
    }

    public async Task SetupAsync(bool joinedChannel = false)
    {
        // Add default behaviours
        AddBehaviour(new LobbyManagerBehaviour());
        AddBehaviour(new MapManagerBehaviour());
        AddBehaviour(new BanBehaviour());
        AddBehaviour(new DebugCommandsBehaviour());
        AddBehaviour(new ConfigBehaviour());

        // Add user specified behaviours
        if (Configuration.Behaviours != null)
        {
            foreach (var behaviourName in Configuration.Behaviours)
            {
                switch (behaviourName)
                {
                    case "AutoHostRotate":
                        AddBehaviour(new AutoHostRotateBehaviour());
                        break;
                    case "AntiAfk":
                        AddBehaviour(new AntiAfkBehaviour());
                        break;
                    case "AutoStart":
                        AddBehaviour(new AutoStartBehaviour());
                        break;
                    case "AbortVote":
                        AddBehaviour(new AbortVoteBehaviour());
                        break;
                    case "Help":
                        AddBehaviour(new HelpBehaviour());
                        break;
                    case "FunCommands":
                        AddBehaviour(new FunCommandsBehaviour());
                        break;
                    default:
                        Log.Error($"Unknown behaviour: {behaviourName}");
                        break;
                }
            }   
        }

        Behaviours.ForEach(x => x.Setup(this));
        
        MultiplayerLobby.OnMatchFinished += () =>
        {
            GamesPlayed++;
        };

        MultiplayerLobby.OnSettingsUpdated += () =>
        {
            // At this point, all behaviours should have done their "recover" stuff, and we may reset the recover status. 
            IsRecovering = false;
        };

        Bot.Client.OnPrivateMessageReceived += ClientOnPrivateMessageReceived;
        Bot.Client.OnPrivateMessageSent += ClientOnPrivateMessageSent;

        if (!joinedChannel)
        {
            IsRecovering = true;

            Bot.Client.OnChannelJoined += channel =>
            {
                if (channel.ChannelName != _channelName) return;
                
                OnLobbyChannelJoined?.Invoke();
            };
            
            // "Temporary" (permanent) fix for the fact that BanchoSharp's JoinChannelAsync calls 
            // OnChannelJoined before Bancho acknowledges that we have joined, so we'll send JOIN manually.
            // And as a result, OnChannelJoined will only get invoked when we've actually joined.
            await Bot.Client.SendAsync($"JOIN {_channelName}");
        }
        else
        {
            OnLobbyChannelJoined?.Invoke();
        }
    }
    
    /// <summary>
    /// Always prefer to send a message to the lobby via this method, as it will make sure everything gets
    /// sent to the rate limiter. You could always call Bot.SendMessage yourself, though.
    /// </summary>
    public void SendMessage(string message)
    {
        Bot.SendMessage(_channelName, message);
    }

    /// <summary>
    /// Get what string to use when passing a player as a parameter in tournament commands.
    /// This will make sure to prioritize player ID, or use player names if not available.
    /// </summary>
    internal string GetPlayerIdentifier(string playerName)
    {
        int? playerId = MultiplayerLobby.Players.FirstOrDefault(x => x.Name == playerName)?.Id;

        return playerId == null ? playerName.Replace(' ', '_') : $"#{playerId}";
    }
    
    private void ClientOnPrivateMessageReceived(IPrivateIrcMessage message)
    {
        if (message.Recipient == _channelName)
        {
            AddMessageToHistory(message);
        }

        // Some behaviours may require Bancho messages sent directly to us, so bypass the
        // channel check.
        if (message.IsBanchoBotMessage)
        {
            OnBanchoMessage?.Invoke(message);
            
            return;
        }

        if (message.Recipient != _channelName)
        {
            return;
        }

        // The bot should probably also allow multiple admins
        // maybe get all referees?
        if (message.Sender == Bot.Configuration.Username)
        {
            OnAdminMessage?.Invoke(message);
        }
        
        OnUserMessage?.Invoke(message);
    }
    
    private void ClientOnPrivateMessageSent(IPrivateIrcMessage e)
    {
        if (e.Recipient != _channelName)
        {
            return;
        }
        
        AddMessageToHistory(e);
        
        // We do this so messages sent from example the WebUI are
        // also processed.
        OnUserMessage?.Invoke(e);
        OnAdminMessage?.Invoke(e);
    }

    private void AddMessageToHistory(IPrivateIrcMessage message)
    {
        if (RecentMessages.Count >= 300)
            RecentMessages.RemoveAt(0);
        
        RecentMessages.Add(message);
    }
    
    private void AddBehaviour(IBotBehaviour behaviour) => Behaviours.Add(behaviour);
}