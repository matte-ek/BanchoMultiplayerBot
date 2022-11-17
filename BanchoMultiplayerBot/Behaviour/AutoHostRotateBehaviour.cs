using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Behaviour;

public class AutoHostRotateBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerSkipVote = null!;
    
    public List<string> Queue { get; private set; } = new();

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerSkipVote = new PlayerVote(_lobby, "Skip host vote");
        
        _lobby.MultiplayerLobby.OnPlayerJoined += player =>
        {
            Queue.Add(player.Name);

            OnQueueUpdated();
        };

        _lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectEventArgs =>
        {
            if (Queue.Contains(disconnectEventArgs.Player.Name))
            {
                Queue.Remove(disconnectEventArgs.Player.Name);

                OnQueueUpdated();
            }
        };

        _lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
        _lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        _lobby.OnUserMessage += OnUserMessage;
    }

    private async void OnUserMessage(IPrivateIrcMessage message)
    {
        if (message.Content.StartsWith("!q") || message.Content.StartsWith("!queue"))
        {
            SendCurrentQueue();

            return;
        }

        if (message.Content.StartsWith("!skip"))
        {
            if (_lobby.MultiplayerLobby.Host is not null)
            {
                if (message.Sender == _lobby.MultiplayerLobby.Host.Name)
                {
                    SkipCurrentPlayer();

                    OnQueueUpdated();

                    return;
                }
            }

            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == message.Sender);
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
        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);
        }

        SkipCurrentPlayer();

        OnQueueUpdated();
        SendCurrentQueue();
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        if (!Queue.Any()) return;
            
        if (player.Name != Queue[0])
        {
            _lobby.SendMessage($"!mp host {Queue[0]}");

            _playerSkipVote.Reset();
        }
    }

    private void OnQueueUpdated()
    {
        if (!Queue.Any()) return;
        
        if (_lobby.MultiplayerLobby.Host is null)
        {
            _lobby.SendMessage($"!mp host {Queue[0]}");
            return;
        }    
        
        if (_lobby.MultiplayerLobby.Host.Name != Queue[0])
        {
            _lobby.SendMessage($"!mp host {Queue[0]}");
        }   
    }

    private void SendCurrentQueue()
    {
        var queueStr = string.Join(", ", Queue.Take(5));

        if (Queue.Count > 5)
            queueStr += "...";

        _lobby.SendMessage($"Queue: {queueStr}");   
    }

    private void SkipCurrentPlayer()
    {
        if (!Queue.Any()) return;
        
        var playerName = Queue[0];
        
        Queue.RemoveAt(0);
        
        // Re-add him back to the end of the queue
        Queue.Add(playerName);   
    }
}