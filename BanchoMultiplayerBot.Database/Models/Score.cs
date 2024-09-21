using BanchoMultiplayerBot.Osu.Data;

namespace BanchoMultiplayerBot.Database.Models;

public class Score
{
    public long Id { get; set; }
    
    // This is only available for osu! scores which meet some criteria 
    public long? OsuScoreId { get; set; }
    
    // This should probably be a foreign key to the lobby table, however, due to
    // the fact that the bot previously didn't use database for lobbies, it's not
    // since the ids would not match.
    public int LobbyId { get; set; }
    
    public long BeatmapId { get; set; }
    
    /// <summary>
    /// osu! player id
    /// </summary>
    public int? PlayerId { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public int GameId { get; set; }
    public virtual Game Game { get; set; } = null!;
    
    public OsuRank Rank { get; set; }
    
    public long TotalScore { get; set; }
    
    public int MaxCombo { get; set; }
    
    public int Count300 { get; set; }
    
    public int Count100 { get; set; }
    
    public int Count50 { get; set; }
    
    public int CountMiss { get; set; }
    
    /// <summary>
    /// Bitset of the mods used for the score, see <see cref="OsuMods"/>
    /// </summary>
    public int Mods { get; set; }

    public DateTime Time { get; set; }
}