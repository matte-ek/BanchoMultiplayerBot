namespace BanchoMultiplayerBot.Data;

public class QueuedMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime Time { get; set; }

    public string Channel { get; init; } = null!;
    public string Content { get; init; } = null!;
}