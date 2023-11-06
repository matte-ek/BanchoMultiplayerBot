namespace BanchoMultiplayerBot.Database.Models;

public class Score
{
    public long Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int? PlayerId { get; set; }
    public int LobbyId { get; set; }
    
    public long? OsuScoreId { get; set; }
    public long BeatmapId { get; set; }
    
    public long TotalScore { get; set; }
    public int Rank { get; set; }
    public int MaxCombo { get; set; }
    public int Count300 { get; set; }
    public int Count100 { get; set; }
    public int Count50 { get; set; }
    public int CountMiss { get; set; }
    public int Mods { get; set; }

    public DateTime Time { get; set; }
}