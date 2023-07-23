namespace BanchoMultiplayerBot.Data;

public class BeatmapInfo
{
    public int Id { get; set; }
    
    public int SetId { get; set; }

    public string Name { get; set; } = string.Empty;
    
    public TimeSpan Length { get; set; }
    
    public float StarRating { get; set; }

}