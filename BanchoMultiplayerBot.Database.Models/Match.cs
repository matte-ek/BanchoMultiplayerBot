namespace BanchoMultiplayerBot.Database.Models;

public class Match
{
    public int Id { get; set; }
    
    public DateTime Created { get; set; }

    public string Configuration { get; set; } = string.Empty;
    
    public string Channel { get; set; } = string.Empty;
    
    public bool Destroyed { get; set; }
}