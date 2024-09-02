using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Data;

public class Vote(string name, string description, ILobby lobby) : IVote
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public ILobby Lobby { get; } = lobby;
    
    public bool IsActive { get; set; }

    public DateTime StartTime { get; set; }
    
    public DateTime PassTime { get; set; }

    public List<string> Votes { get; set; } = [];
    
    public bool PlayerVote(MultiplayerPlayer player)
    {
        if (Lobby.MultiplayerLobby == null)
        {
            Log.Warning("Vote: Multiplayer lobby is null");
            
            return false;
        }
        
        // If this is the first vote, set the start time and activate the vote.
        if (!IsActive)
        {
            Votes.Clear();

            StartTime = DateTime.UtcNow;
            IsActive = true;
        }

        // Make sure our current votes are valid.
        ValidateVotes();
        
        if (!Votes.Contains(player.Name))
        {
            Votes.Add(player.Name);
        }
        
        int requiredVotes = Math.Max(Lobby.MultiplayerLobby!.Players.Count / 2 + 1, 1);
        
        if (Votes.Count >= requiredVotes)
        {
            Log.Verbose("Vote: Passed vote {Vote} successfully", Name);

            Lobby.BanchoConnection.MessageHandler.SendMessage(Lobby.MultiplayerLobby!.ChannelName, $"{Description} vote passed! ({Votes.Count}/{requiredVotes})");

            IsActive = false;
            PassTime = DateTime.UtcNow;
            Votes.Clear();

            return true;
        }

        Lobby.BanchoConnection.MessageHandler.SendMessage(Lobby.MultiplayerLobby!.ChannelName, $"{Description} vote ({Votes.Count}/{requiredVotes})");

        return false;
    }
    
    public void Abort()
    {
        Log.Verbose("Vote: Aborted vote {Vote} with {Votes} vote(s)", Name, Votes.Count);

        IsActive = false;
        Votes.Clear();
    }

    /// <summary>
    /// Will validate the votes and remove any votes that are not in the current lobby.
    /// </summary>
    private void ValidateVotes()
    {
        if (Lobby.MultiplayerLobby == null)
        {
            return;
        }

        foreach (var vote in Votes.ToList().Where(vote => Lobby.MultiplayerLobby.Players.All(x => x.Name != vote)))
        {
            Log.Verbose("Vote: Removed player '{PlayerName}' from vote {Vote}, player disconnected", vote, Name);

            Votes.Remove(vote);
        }
    }
}