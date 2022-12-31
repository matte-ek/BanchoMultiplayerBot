namespace BanchoMultiplayerBot;

public class QueuedMessage
{
    public DateTime Time { get; set; }
    
    public string Channel { get; init; } = null!;
    public string Content { get; init; } = null!;
}