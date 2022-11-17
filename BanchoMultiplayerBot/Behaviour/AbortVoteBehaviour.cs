using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class AbortVoteBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerAbortVote = null!;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerAbortVote = new PlayerVote(_lobby, "Abort the match vote");

        _lobby.MultiplayerLobby.OnMatchFinished += () =>
        {
            _playerAbortVote.Reset();
        };
        
        _lobby.MultiplayerLobby.OnMatchAborted += () =>
        {
            _playerAbortVote.Reset();
        };
        
        _lobby.OnUserMessage += OnUserMessage;
    }

    private void OnUserMessage(IPrivateIrcMessage message)
    {
        if (message.Content.StartsWith("!abort"))
        {
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == message.Sender);
            if (player is not null)
            {
                if (_playerAbortVote.Vote(player))
                {
                    _lobby.SendMessage("!mp abort");
                }
            }
        }
    }
}