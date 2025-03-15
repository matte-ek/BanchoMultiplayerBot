using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Interfaces;

public interface IVote
{
    /// <summary>
    /// A name to keep track of the vote
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// User facing description of the vote, will be displayed in the chat
    /// if the vote count is increased or is passed. Example: "Skip the host"
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Which lobby this vote is associated with.
    /// </summary>
    public ILobby Lobby { get; }
    
    /// <summary>
    /// If the vote is currently active or not.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whenever the vote was started.
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Whenever the vote was passed.
    /// </summary>
    public DateTime PassTime { get; set; }
    
    /// <summary>
    /// A list of player names who have voted.
    /// </summary>
    public List<string> Votes { get; set; }
    
    /// <summary>
    /// Used to cast a vote for a player. If the vote passes, the vote will be reset
    /// and the method will return true. Otherwise, the vote will be registered and the method
    /// will return false.
    /// </summary>
    public bool PlayerVote(MultiplayerPlayer player);
    
    /// <summary>
    /// Used to reset the vote, and clear all votes.
    /// </summary>
    public void Abort();
}