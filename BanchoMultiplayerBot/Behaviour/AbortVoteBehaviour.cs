using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Utilities;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// Nothing complicated, manages the '!abort' command
/// </summary>
public class AbortVoteBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerAbortVote = null!;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerAbortVote = new PlayerVote(_lobby, "Abort the match vote");

        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        _lobby.MultiplayerLobby.OnMatchAborted += OnMatchAborted;
        _lobby.OnUserMessage += OnUserMessage;
    }

    public void Shutdown()
    {
        _lobby.MultiplayerLobby.OnMatchFinished -= OnMatchFinished;
        _lobby.MultiplayerLobby.OnMatchAborted -= OnMatchAborted;
        _lobby.OnUserMessage -= OnUserMessage;
        
        _playerAbortVote = null!;
    }

    private void OnMatchFinished()
    {
        _playerAbortVote.Reset();
    }

    private void OnMatchAborted()
    {
        _playerAbortVote.Reset();
    }

    private void OnUserMessage(PlayerMessage message)
    {
        if (!message.Content.ToLower().StartsWith("!abort"))
            return;

        if (!_playerAbortVote.Vote(message, message.BanchoPlayer))
        {
            return;
        }
        
        _lobby.SendMessage("!mp abort");
    }
}