namespace BanchoMultiplayerBot.Data;

public class BotRuntimeInfo
{
    /// <summary>
    /// Time when bot started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// If the bot had to restart and restore itself due to a connection error
    /// </summary>
    public bool HadNetworkConnectionIssue { get; set; }
    
    /// <summary>
    /// Time when bot last had networking issues
    /// </summary>
    public DateTime LastConnectionIssueTime { get; set; }
    
    public int MessagesSent { get; set; }
    public int MessagesReceived { get; set; }
    
    public int ApiRequests { get; set; }
    public int ApiErrors { get; set; }
    
}