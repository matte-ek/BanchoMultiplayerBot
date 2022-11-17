namespace BanchoMultiplayerBot;

public class QueuedMessage
{
    public DateTime Time { get; set; }
    public string Channel { get; set; } = null!;
    public string Content { get; set; } = null!;
}