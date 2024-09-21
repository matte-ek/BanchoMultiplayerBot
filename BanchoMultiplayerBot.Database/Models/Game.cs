namespace BanchoMultiplayerBot.Database.Models;

public class Game
{
    public int Id { get; set; }
    
    /// <summary>
    /// The osu! beatmap id
    /// </summary>
    public long BeatmapId { get; set; }
    
    /// <summary>
    /// Amount of players that were present at the beginning of the game
    /// </summary>
    public int PlayerCount { get; set; }
    
    /// <summary>
    /// Amount of players that finished the game
    /// </summary>
    public int PlayerFinishCount { get; set; }
    
    /// <summary>
    /// Amount of players that passed the map
    /// </summary>
    public int PlayerPassedCount { get; set; }
    
    /// <summary>
    /// Time when the match ended
    /// </summary>
    public DateTime Time { get; set; }
}