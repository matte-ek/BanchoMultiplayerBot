using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class Vote(string name, ILobby lobby) : IVote
{
    public string Name { get; } = name;

    public ILobby Lobby { get; } = lobby;
    
    public bool IsActive { get; set; }

    public DateTime StartTime { get; set; }

    public List<string> Votes { get; set; } = new();
    
    public bool PlayerVote(MultiplayerPlayer player)
    {
        if (Lobby.MultiplayerLobby == null)
        {
            return false;
        }
        
        if (!IsActive)
        {
            Votes.Clear();
            
            StartTime = DateTime.UtcNow;
            IsActive = true;
        }

        ValidateVotes();
        
        if (!Votes.Contains(player.Name))
        {
            Votes.Add(player.Name);
        }
        
        int requiredVotes = Math.Max(Lobby.MultiplayerLobby!.Players.Count / 2 + 1, 1);
        
        if (Votes.Count >= requiredVotes)
        {
            IsActive = false;
            return true;
        }
        
        return false;
    }
    
    public void Abort()
    {
        IsActive = false;
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
            Votes.Remove(vote);
        }
    }
}