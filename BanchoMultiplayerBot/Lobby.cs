using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Config;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;

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
    /// If the lobby has been removed by bancho
    /// </summary>
    public bool IsParted { get; set; }

    /// <summary>
    /// List of the 300 recent messages in the lobby, primarily used within the WebUI
    /// </summary>
    public List<IPrivateIrcMessage> RecentMessages { get; } = new();
    
    public event Action? OnLobbyChannelJoined;
    public event Action<PlayerMessage>? OnUserMessage;
    public event Action<PlayerMessage>? OnAdminMessage;
    public event Action<IPrivateIrcMessage>? OnBanchoMessage;
    
    public string Channel { get; set; }

    internal int LobbyIndex = 0;
    internal string LobbyLabel = string.Empty;
    
    public Lobby(Bot bot, LobbyConfiguration configuration, string channel)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = new MultiplayerLobby(Bot.Client, long.Parse(channel[4..]), channel);
        Channel = channel;
    }
    
    public Lobby(Bot bot, LobbyConfiguration configuration, MultiplayerLobby lobby)
    {
        Bot = bot;
        Configuration = configuration;
        MultiplayerLobby = lobby;
        Channel = lobby.ChannelName;
    }

    public async Task SetupAsync(bool joinedChannel = false)
    {
        // Add default behaviours
        AddBehaviour(new LobbyManagerBehaviour());
        AddBehaviour(new MapManagerBehaviour());
        AddBehaviour(new BanBehaviour());
        AddBehaviour(new DebugCommandsBehaviour());
        AddBehaviour(new ConfigBehaviour());
        AddBehaviour(new StatisticsBehaviour());

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
                    case "FunCommands":
                        AddBehaviour(new FunCommandsBehaviour());
                        break;
                    default:
                        Log.Error($"Unknown behaviour: {behaviourName}");
                        break;
                }
            }   
        }

        LobbyIndex = Bot.Lobbies.FindIndex(x => x == this);
        LobbyLabel = Bot.Lobbies.FindIndex(x => x == this).ToString();

        Behaviours.ForEach(x => x.Setup(this));
        
        MultiplayerLobby.OnSettingsUpdated += () =>
        {
            // At this point, all behaviours should have done their "recover" stuff, and we may reset the recover status. 
            IsRecovering = false;
            
            // We also need to reset the map manager's status here, since it needs to be at the last "OnSettingsUpdated" event listener.
            var mapManagerBehaviour = Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
            if (mapManagerBehaviour != null)
            {
                ((MapManagerBehaviour)mapManagerBehaviour).MapValidationStatus = MapManagerBehaviour.MapValidation.None;
            }
        };

        Bot.Client.OnPrivateMessageReceived += ClientOnPrivateMessageReceived;
        Bot.Client.OnPrivateMessageSent += ClientOnPrivateMessageSent;

        if (!joinedChannel)
        {
            IsRecovering = true;

            Bot.Client.OnChannelJoined += channel =>
            {
                if (channel.ChannelName != Channel) return;
                
                OnLobbyChannelJoined?.Invoke();
            };
            
            // "Temporary" (permanent) fix for the fact that BanchoSharp's JoinChannelAsync calls 
            // OnChannelJoined before Bancho acknowledges that we have joined, so we'll send JOIN manually.
            // And as a result, OnChannelJoined will only get invoked when we've actually joined.
            await Bot.Client.SendAsync($"JOIN {Channel}");
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
        Bot.SendMessage(Channel, message);
    }

    /// <summary>
    /// Runs "!mp settings" as per usual, but adds zero width spaces to avoid stupid Bancho anti-spam stuff.
    /// </summary>
    public void UpdateSettings()
    {
        Bot.SendMessage(Channel,  $"!mp settings {string.Join("", Enumerable.Repeat("\u200B", LobbyIndex))}");
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
    
    private async void ClientOnPrivateMessageReceived(IPrivateIrcMessage message)
    {
        if (message.Recipient == Channel)
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

        var isAdministrator = await Bot.IsAdministrator(message.Sender);
        var playerMessage = new PlayerMessage(message.RawMessage, this, isAdministrator);

        if (message.Recipient != Channel)
        {
            if (message.IsDirect)
            {
                // If the player is in this current lobby, we'll try to handle the command anyway,
                // since a lot of people tend to try to use some of these commands in DMs.
                if (MultiplayerLobby.Players.Any(x => x.Name.ToIrcNameFormat() == message.Sender.ToIrcNameFormat()))
                {
                    OnUserMessage?.Invoke(playerMessage);

                    return;
                }
            }
            
            return;
        }

        if (isAdministrator)
        {
            OnAdminMessage?.Invoke(playerMessage);
        }
        
        OnUserMessage?.Invoke(playerMessage);
    }
    
    private void ClientOnPrivateMessageSent(IPrivateIrcMessage e)
    {
        if (e.Recipient != Channel)
        {
            return;
        }
        
        AddMessageToHistory(e);

        var playerMessage = new PlayerMessage(e.RawMessage, this, true);
        
        // We do this so messages sent from example the WebUI are
        // also processed.
        OnUserMessage?.Invoke(playerMessage);
        OnAdminMessage?.Invoke(playerMessage);
    }

    private void AddMessageToHistory(IPrivateIrcMessage message)
    {
        if (RecentMessages.Count >= 600)
            RecentMessages.RemoveAt(0);
        
        RecentMessages.Add(message);
    }

    private void AddBehaviour(IBotBehaviour behaviour) => Behaviours.Add(behaviour);
}