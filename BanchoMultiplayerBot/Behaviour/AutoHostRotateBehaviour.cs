using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Behaviour;

public class AutoHostRotateBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    public List<string> Queue { get; private set; } = new();
     
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnPlayerJoined += async player =>
        {
            Queue.Add(player.Name);

            await OnQueueUpdated();
        };

        _lobby.MultiplayerLobby.OnPlayerDisconnected += async disconnectEventArgs =>
        {
            if (Queue.Contains(disconnectEventArgs.Player.Name))
            {
                Queue.Remove(disconnectEventArgs.Player.Name);

                await OnQueueUpdated();
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
            await SendCurrentQueue();

            return;
        }

        if (message.Content.StartsWith("!skip"))
        {
            if (_lobby.MultiplayerLobby.Host is not null)
            {
                if (message.Sender == _lobby.MultiplayerLobby.Host.Name)
                {
                    SkipCurrentPlayer();

                    await OnQueueUpdated();

                    return;
                }
            }
            
            // TODO: Player vote skip
        }
    }

    private async void OnSettingsUpdated()
    {
        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            if (!Queue.Contains(player.Name))
                Queue.Add(player.Name);
        }

        SkipCurrentPlayer();

        await OnQueueUpdated();
        await SendCurrentQueue();
    }

    private async void OnHostChanged(MultiplayerPlayer player)
    {
        if (!Queue.Any()) return;
            
        if (player.Name != Queue[0])
        {
            await _lobby.SendMessageAsync($"!mp host {Queue[0]}");
        }
    }

    private async Task OnQueueUpdated()
    {
        if (!Queue.Any()) return;
        if (_lobby.MultiplayerLobby.Host is null) return;    
        
        if (_lobby.MultiplayerLobby.Host.Name != Queue[0])
        {
            await _lobby.SendMessageAsync($"!mp host {Queue[0]}");
        }   
    }

    private async Task SendCurrentQueue()
    {
        var queueStr = string.Join(", ", Queue, 0, 5);

        if (Queue.Count > 5)
            queueStr += "...";

        await _lobby.SendMessageAsync($"Queue: {queueStr}");   
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