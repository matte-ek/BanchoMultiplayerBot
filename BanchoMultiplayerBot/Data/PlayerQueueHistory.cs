namespace BanchoMultiplayerBot.Data;

public class PlayerQueueHistory
{
    public string Name { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public int QueuePosition { get; set; } = -1;
}