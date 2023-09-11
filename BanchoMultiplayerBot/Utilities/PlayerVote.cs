using BanchoMultiplayerBot.Data;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Utility to easily create a player vote
/// </summary>
public class PlayerVote
{
    public string Question { get; }
    public Lobby Lobby { get; }
    
    public List<MultiplayerPlayer> Votes { get; } = new();
    
    public PlayerVote(Lobby lobby, string question)
    {
        Lobby = lobby;
        Question = question;

        Lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectArgs =>
        {
            Votes.Remove(disconnectArgs.Player);
        };
    }

    public bool Vote(PlayerMessage message, MultiplayerPlayer player)
    {
        int requiredVotes = Math.Max(Lobby.MultiplayerLobby.Players.Count / 2 + 1, 1);

        if (Votes.Contains(player))
        {
            return false;
        }
        
        Votes.Add(player);
        
        if (Votes.Count < requiredVotes)
        {
            message.Reply($"{Question} ({Votes.Count}/{requiredVotes})", true);

            return false;
        }
        
        message.Reply($"{Question} passed ({Votes.Count}/{requiredVotes})", true);

        Log.Information($"Passed vote {Question} with ({Votes.Count}/{requiredVotes})");

        Reset();
        
        return true;
    }

    public void Reset()
    {
        Votes.Clear();
    }
    
}