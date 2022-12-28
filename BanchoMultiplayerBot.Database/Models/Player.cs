namespace BanchoMultiplayerBot.Database.Models;

public class Player
{

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int TotalFirstPlaceCount { get; set; }

    public int TotalPlaytime { get; set; }
    
}