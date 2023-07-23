namespace BanchoMultiplayerBot.Data;

public class PlayerQueueHistory
{
    public string Name { get; init; } = string.Empty;
    public DateTime Time { get; init; }
    public int QueuePosition { get; init; } = -1;
}