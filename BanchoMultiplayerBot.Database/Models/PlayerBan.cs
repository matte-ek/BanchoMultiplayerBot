namespace BanchoMultiplayerBot.Database.Models;

public class PlayerBan
{
    public int Id { get; set; }

    public bool Active { get; set; } = true;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Optional ban reason for the player
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Time the player got banned
    /// </summary>
    public DateTime Time { get; set; }
    
    /// <summary>
    /// Time when the ban will expire, if any.
    /// </summary>
    public DateTime? Expire { get; set; }
    
    /// <summary>
    /// If true, the player may still join lobbies but aren't able to become host.
    /// </summary>
    public bool HostBan { get; set; }
    
}