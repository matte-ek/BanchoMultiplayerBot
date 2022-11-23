using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot;

public class PlayerVote
{
    
    public string Question { get; private set; }
    public Lobby Lobby { get; private set; }
    public List<MultiplayerPlayer> Votes { get; private set; } = new();

    public event Action? OnVotePassed; 

    public PlayerVote(Lobby lobby, string question)
    {
        Lobby = lobby;
        Question = question;

        Lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectArgs =>
        {
            Votes.Remove(disconnectArgs.Player);
        };
    }

    public bool Vote(MultiplayerPlayer player)
    {
        int requiredVotes = Math.Max(Lobby.MultiplayerLobby.Players.Count / 2 + 1, 1);

        if (Votes.Contains(player))
        {
            return false;
        }
        
        Votes.Add(player);
        
        Lobby.SendMessage($"{Question} ({Votes.Count}/{requiredVotes})");

        if (Votes.Count < requiredVotes) 
            return false;

        Reset();
        
        OnVotePassed?.Invoke();
            
        return true;
    }

    public void Reset()
    {
        Votes.Clear();
    }
    
}