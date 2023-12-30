using BanchoMultiplayerBot.Extensions;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;
using System.Xml.Linq;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Utilities;
using MudBlazor.Services;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will manage a queue and pass the host around, so everyone gets a chance to 
/// pick a map. 
/// </summary>
public class AutoHostRotateBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerSkipVote = null!;

    private bool _matchInProgress;

    private bool _hasSkippedHost;

    // Keep track of the last 5 players who left, and their queue position. We do this so we are able
    // to restore people's queue position if they accidentally left and is rejoining within 30 seconds.
    private readonly List<PlayerQueueHistory> _recentLeaveHistory = new();

    private MapManagerBehaviour? _mapManagerBehaviour;

    // Players here are banned, and shouldn't be added to the queue.
    // This list exists just to cache the names, to avoid having to do DB lookups all the time.
    private readonly List<string> _queueIgnorePlayers = new();
    
    public List<string> Queue { get; } = new();

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerSkipVote = new PlayerVote(_lobby, "Skip host vote");

        _lobby.MultiplayerLobby.OnPlayerJoined += async player =>
        {
            if ((await BanBehaviour.GetActivePlayerBans(player.Name)).Any())
            {
                // To make sure they aren't added afterwards.
                _queueIgnorePlayers.Add(player.Name);
                
                return;
            }
            
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);

            if (_lobby.IsRecovering)
            {
                return;
            }
            
            RestorePlayerQueuePosition(player);

            OnQueueUpdated();
        };

        _lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectEventArgs =>
        {
            if (_queueIgnorePlayers.Contains(disconnectEventArgs.Player.Name))
            {
                _queueIgnorePlayers.Remove(disconnectEventArgs.Player.Name);
            }
            
            if (Queue.Contains(disconnectEventArgs.Player.Name))
            {
                AddLeaveQueuePosition(disconnectEventArgs.Player.Name);
                
                Queue.Remove(disconnectEventArgs.Player.Name);

                OnQueueUpdated();

                if (_lobby.MultiplayerLobby.Host is not null && _lobby.MultiplayerLobby.Host.Name == disconnectEventArgs.Player.Name && _matchInProgress)
                {
                    _hasSkippedHost = true;
                }
            }
        };

        _lobby.MultiplayerLobby.OnMatchStarted += () =>
        {
            _hasSkippedHost = false;
            _matchInProgress = true;
        };

        _lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
        _lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        _lobby.OnUserMessage += OnUserMessage;
        _lobby.OnAdminMessage += OnAdminMessage;
        
        var mapManagerBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
        if (mapManagerBehaviour != null)
        {
            _mapManagerBehaviour = ((MapManagerBehaviour)mapManagerBehaviour);
        }
    }

    public void SkipCurrentHost()
    {
        SkipCurrentPlayer();
        OnQueueUpdated();
    }

    public void SetNewHost(MultiplayerPlayer player)
    {
        var name = player.Name;

        if (Queue.Contains(name))
        {
            Queue.Remove(name);
        }

        Queue.Insert(0, name);

        OnQueueUpdated();
    }

    public void MovePlayer(MultiplayerPlayer player, int newPosition)
    {
        var name = player.Name;

        if (Queue.Contains(name))
        {
            Queue.Remove(name);
        }

        Queue.Insert(newPosition, name);

        OnQueueUpdated();
    }

    public async Task RefreshPlayerBanStates()
    {
        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            if (!(await BanBehaviour.GetActivePlayerBans(player.Name)).Any()) 
                continue;
            
            // To make sure they aren't added afterwards.
            _queueIgnorePlayers.Add(player.Name);
            
            if (Queue.Contains(player.Name))
                Queue.Remove(player.Name);
        }
    }
    
    private async void OnUserMessage(PlayerMessage message)
    {
        // Show the current queue to the user.
        if (message.Content.ToLower().Equals("!q") || message.Content.ToLower().Equals("!queue"))
        {
            message.Reply(GetCurrentQueueMessage());

            return;
        }

        // Shows the user's position in the queue.
        if (message.Content.ToLower().StartsWith("!queuepos") || message.Content.ToLower().StartsWith("!qp"))
        {
            var queuePosition = Queue.FindIndex(x => x.ToIrcNameFormat().Equals(message.Sender));

            message.Reply(queuePosition == -1
                ? "Couldn't find player in queue."
                : $"Queue position for {message.Sender}: #{(queuePosition + 1).ToString()}");

            return;
        }

        // Allow players to vote to skip the current host, or allow the host to skip themselves.
        if (message.Content.ToLower().StartsWith("!skip") || message.Content.ToLower().Equals("!s"))
        {
            // If the host is sending the message, just skip.
            if (_lobby.MultiplayerLobby.Host is not null)
            {
                if (message.Sender == _lobby.MultiplayerLobby.Host.Name.ToIrcNameFormat())
                {
                    SkipCurrentPlayer();
                    OnQueueUpdated();

                    _playerSkipVote.Reset();

                    return;
                }
            }

            // If the player isn't host, start a vote.
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == message.Sender);
            if (player is not null)
            {
                if (_playerSkipVote.Vote(message, player))
                {
                    SkipCurrentPlayer();

                    OnQueueUpdated();

                    _lobby.Bot.RuntimeInfo.Statistics.HostSkipCount.WithLabels(_lobby.LobbyLabel).Inc();

                    return;
                }
            }
        }

        // Allow the user to enable/disable auto-skip.
        if (message.Content.StartsWith("!autoskip"))
        {
            try
            {
                if (message.BanchoPlayer is null)
                {
                    return;
                }
                
                using var userRepository = new UserRepository();

                var user = await userRepository.FindOrCreateUser(message.BanchoPlayer.Name);
                var previousStatus = user.AutoSkipEnabled;
                
                if (message.Content.StartsWith("!autoskip "))
                {
                    if (message.Content.EndsWith("enable") || message.Content.EndsWith("on"))
                        user.AutoSkipEnabled = true;
                    if (message.Content.EndsWith("disable") || message.Content.EndsWith("off"))
                        user.AutoSkipEnabled = false;
                }
                
                var status = user.AutoSkipEnabled ? "enabled" : "disabled";

                message.Reply(previousStatus != user.AutoSkipEnabled
                    ? $"{message.BanchoPlayer.Name}, your auto-skip has been {status}."
                    : $"{message.BanchoPlayer.Name}, your auto-skip is {status}.");

                await userRepository.Save();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private void OnAdminMessage(PlayerMessage message)
    {
        if (message.Content.StartsWith("!forceskip"))
        {
            SkipCurrentPlayer();
            OnQueueUpdated();
            
            _lobby.Bot.RuntimeInfo.Statistics.HostSkipCount.WithLabels(_lobby.LobbyLabel).Inc();
        }

        if (message.Content.StartsWith("!sethost "))
        {
            try
            {
                var name = message.Content.Split("!sethost ")[1];
                var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat().ToLower() == name.ToLower() || x.Name.ToLower() == name.ToLower());

                if (player is null)
                {
                    _lobby.SendMessage("Failed to find player.");
                }
                else
                {
                    SetNewHost(player);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        if (message.Content.StartsWith("!setqueuepos "))
        {
            try
            {
                var split = message.Content.Split(' ');
                var targetName = split[1];
                var targetQueuePosition = int.Parse(split[2]);
                var targetPlayer = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat().ToLower() == targetName.ToLower());

                // Make sure the target position is valid.
                if (0 > targetQueuePosition || targetQueuePosition >= Queue.Count)
                {
                    _lobby.SendMessage("Target position is out of range.");
                }
                else
                {
                    if (targetPlayer is null)
                    {
                        _lobby.SendMessage("Failed to find player! Make sure to replace spaces with underscores.");
                    }
                    else
                    {
                        MovePlayer(targetPlayer, targetQueuePosition);
                    }   
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private void OnSettingsUpdated()
    {
        // Don't do anything if a map is being validated.
        if (_mapManagerBehaviour?.MapValidationStatus != MapManagerBehaviour.MapValidation.None)
        {
            return;
        }
        
        // Attempt to reload the old queue if we're recovering a previous session.
        if (_lobby.IsRecovering && _lobby.Configuration.PreviousQueue != null)
        {
            Queue.Clear();

            // Add players that are still in the lobby to the queue.
            foreach (var player in _lobby.Configuration.PreviousQueue.Split(','))
            {
                if (_lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == player) is not null && !Queue.Contains(player))
                {
                    Queue.Add(player);
                }
            }

            Log.Information($"Recovered old queue: {string.Join(", ", Queue.Take(5))}");
        }
        
        // In some rare cases, players which have already left remain in the queue, so 
        // go through the queue just in case.
        foreach (var player in Queue.ToList())
        {
            if (_lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == player) is not null) 
                continue;

            Log.Warning($"Disconnected player {player} in queue!");
            Queue.Remove(player);
        }
        
        // Same deal here, but sometimes players aren't in the queue.
        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            // Maybe they shouldn't?
            if (_queueIgnorePlayers.Any(x => x == player.Name))
                continue;
            
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);
        }
        
        // Don't skip a player if we're just restoring a previous session.
        if (_lobby.IsRecovering)
        {
            return;
        }

        if (!_hasSkippedHost)
        {
            SkipCurrentPlayer();
        }

        OnQueueUpdated();
        
        _lobby.SendMessage(GetCurrentQueueMessage(true));

        _matchInProgress = false;
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        if (!Queue.Any()) 
            return;
        if (_lobby.IsRecovering) 
            return;

        if (player.Name != Queue[0])
        {
            SetHost(Queue[0]);
        }
    }

    private async void OnQueueUpdated()
    {
        if (!Queue.Any()) 
            return;

        try
        {
            using var userRepository = new UserRepository();
            
            // Create a backup as SkipCurrentPlayer() is going to be moving things around
            var queue = Queue.ToArray(); 
            
            var user = await userRepository.FindOrCreateUser(queue[0]);
            if (user.AutoSkipEnabled)
            {
                SkipCurrentPlayer();
                
                // This feels sort of stupid, but I would like to try to avoid doing any recursion here,
                // and I also want to properly handle situations as in "what if everyone has auto-skip enabled?", or
                // "what if three players in a row have auto skip enabled?", which should be fine here.
                foreach (var player in queue.Skip(1))
                {
                    user = await userRepository.FindOrCreateUser(player);

                    if (user.AutoSkipEnabled)
                    {
                        SkipCurrentPlayer();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        if (_lobby.MultiplayerLobby.Host is null)
        {
            SetHost(Queue[0]);
            return;
        }

        if (_lobby.MultiplayerLobby.Host.Name != Queue[0])
        {
            SetHost(Queue[0]);
        }
    }

    /// <summary>
    /// Send the first 5 people in the queue in the lobby chat. The player names will include a 
    /// zero width space to avoid tagging people.
    /// </summary>
    private string GetCurrentQueueMessage(bool tagHost = false)
    {
        var queueStr = "";
        var cleanPlayerNamesQueue = new List<string>();

        // Add a zero width space to the player names to avoid mentioning them
        Queue.ForEach(playerName => cleanPlayerNamesQueue.Add($"{playerName[0]}\u200B{playerName[1..]}"));
        
        // Replace the host with the original name, if requested.
        if (tagHost && cleanPlayerNamesQueue.Any())
        {
            cleanPlayerNamesQueue.RemoveAt(0);
            cleanPlayerNamesQueue.Insert(0, Queue.First());
        }
        
        // Compile a queue string that is shorter than 100 characters.
        foreach (var name in cleanPlayerNamesQueue)
        {
            if (queueStr.Length > 100)
            {
                queueStr = queueStr[..^2] + "...";
                break;
            }

            queueStr += name + (name != cleanPlayerNamesQueue.Last() ? ", " : string.Empty);
        }

        return $"Queue: {queueStr}";
    }

    /// <summary>
    /// Skips the first user in the queue, will NOT automatically update host.
    /// </summary>
    private void SkipCurrentPlayer()
    {
        if (!Queue.Any()) return;

        var playerName = Queue[0];

        Queue.RemoveAt(0);

        // Re-add the player to the end of the queue
        Queue.Add(playerName);

        _hasSkippedHost = false;
        _playerSkipVote.Reset();
    }

    private void AddLeaveQueuePosition(string player)
    {
        _recentLeaveHistory.Insert(0, new PlayerQueueHistory()
        {
            Name = player,
            QueuePosition = Queue.IndexOf(player),
            Time = DateTime.Now
        });
                
        if (_recentLeaveHistory.Count > 5)
            _recentLeaveHistory.RemoveAt(_recentLeaveHistory.Count - 1);
    }
    
    private void RestorePlayerQueuePosition(MultiplayerPlayer player)
    {
        try
        {
            var previousQueuePosition = _recentLeaveHistory.Where(x => x.Name == player.Name)?.FirstOrDefault();
            if (previousQueuePosition == null)
                return;
        
            // Don't restore if the player was host
            if (previousQueuePosition.QueuePosition == 0)
                return;
        
            // Make sure this was recent, as what would happen if accidentally disconnected/crashed.
            if (DateTime.Now >= previousQueuePosition.Time.AddSeconds(60))
                return;
            
            if (0 > previousQueuePosition.QueuePosition || previousQueuePosition.QueuePosition >= Queue.Count)
                return;
            
            MovePlayer(player, previousQueuePosition.QueuePosition);
            
            Log.Information($"Restored re-connected player queue position to #{previousQueuePosition.QueuePosition + 1}");

            _recentLeaveHistory.Remove(previousQueuePosition);
        }
        catch (Exception e)
        {
            // ignored.
        }
    }
    
    private void SetHost(string playerName)
    {
        _lobby.SendMessage($"!mp host {_lobby.GetPlayerIdentifier(playerName)}");
    }
}