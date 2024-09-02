using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Interfaces;

public interface IVote
{
    public string Name { get; }

    public string Description { get; }

    public ILobby Lobby { get; }
    
    public bool IsActive { get; set; }

    public DateTime StartTime { get; set; }
    
    public DateTime PassTime { get; set; }
    
    public List<string> Votes { get; set; }
    
    public bool PlayerVote(MultiplayerPlayer player);
    
    public void Abort();
}