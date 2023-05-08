using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Interfaces;

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
        if (!message.Content.ToLower().StartsWith("!abort"))
            return;

        var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == message.Sender);

        if (player is null)
            return;

        if (_playerAbortVote.Vote(player))
        {
            _lobby.SendMessage("!mp abort");
        }
    }
}