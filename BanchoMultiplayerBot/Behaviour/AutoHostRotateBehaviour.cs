using BanchoMultiplayerBot.Extensions;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;
using System.Xml.Linq;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will manage a queue and pass the host around, so everyone gets a chance to 
/// pick a map. 
/// </summary>
public class AutoHostRotateBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerSkipVote = null!;

    private bool _hasSkippedHost;

    private bool _matchInProgress;

    public List<string> Queue { get; } = new();

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerSkipVote = new PlayerVote(_lobby, "Skip host vote");

        _lobby.MultiplayerLobby.OnPlayerJoined += player =>
        {
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);

            if (_lobby.IsRecovering)
                return;

            OnQueueUpdated();
        };

        _lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectEventArgs =>
        {
            if (Queue.Contains(disconnectEventArgs.Player.Name))
            {
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
        _lobby.OnBanchoMessage += OnBanchoMessage;
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
    
    private void OnBanchoMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.Equals("User not found"))
        {
            Log.Warning("Bancho couldn't find a targeted user!");
        }
    }
    
    private void OnAdminMessage(IPrivateIrcMessage message)
    {
        if (message.Content.StartsWith("!forceskip"))
        {
            SkipCurrentPlayer();
            OnQueueUpdated();
        }

        if (message.Content.StartsWith("!sethost "))
        {
            try
            {
                var name = message.Content.Split("!sethost ")[1];
                var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat().ToLower() == name.ToLower());

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
    }

    private void OnUserMessage(IPrivateIrcMessage message)
    {
        // Allow the users to see the current queue
        if (message.Content.ToLower().Equals("!q") || message.Content.ToLower().Equals("!queue"))
        {
            SendCurrentQueue();

            return;
        }

        try
        {
            if (message.Content.ToLower().StartsWith("!queuepos"))
            {
                var targetName = message.Sender;
                
                if (message.Content.StartsWith("!queuepos ")) 
                    targetName = message.Content.Split("!queuepos ")[1];
                
                var queuePosition = Queue.FindIndex(x => x.ToIrcNameFormat().Equals(targetName));

                _lobby.SendMessage(queuePosition == -1
                    ? "Couldn't find player in queue." // Don't really wanna echo back user input, so don't include the player name here.
                    : $"Queue position for {targetName}: #{(queuePosition + 1).ToString()}");

                return;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        if (message.Content.ToLower().StartsWith("!skip"))
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
                if (_playerSkipVote.Vote(player))
                {
                    SkipCurrentPlayer();

                    OnQueueUpdated();

                    return;
                }
            }
        }
    }

    private void OnSettingsUpdated()
    {
        // Attempt to reload the old queue if we're recovering a previous session.
        if (_lobby.IsRecovering && _lobby.Configuration.PreviousQueue != null)
        {
            Queue.Clear();

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
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);
        }
        
        // Don't skip a player if we're just restoring a previous session.
        if (_lobby.IsRecovering)
        {
            return;
        }

        if (!_hasSkippedHost)
            SkipCurrentPlayer();

        OnQueueUpdated();
        SendCurrentQueue();

        _matchInProgress = false;
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        if (!Queue.Any()) return;

        if (_lobby.IsRecovering)
            return;

        if (player.Name != Queue[0])
        {
            SetHost(Queue[0]);
        }
    }

    private void OnQueueUpdated()
    {
        if (!Queue.Any()) return;

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
    private void SendCurrentQueue()
    {
        var cleanPlayerNamesQueue = new List<string>();

        // Add a zero width space to the player names to avoid mentioning them
        foreach (var playerName in Queue.Take(5))
        {
            cleanPlayerNamesQueue.Add($"{playerName[0]}\u200B{playerName.Substring(1)}");
        }

        var queueStr = string.Join(", ", cleanPlayerNamesQueue.Take(5));

        if (Queue.Count > 5)
            queueStr += "...";

        _lobby.SendMessage($"Queue: {queueStr}");
    }

    /// <summary>
    /// Skips the first user in the queue, will NOT automatically update host.
    /// </summary>
    private void SkipCurrentPlayer()
    {
        if (!Queue.Any()) return;

        var playerName = Queue[0];

        Queue.RemoveAt(0);

        // Re-add him back to the end of the queue
        Queue.Add(playerName);

        _hasSkippedHost = false;
        _playerSkipVote.Reset();
    }

    private void SetHost(string playerName)
    {
        _lobby.SendMessage($"!mp host {_lobby.GetPlayerIdentifier(playerName)}");
    }
}