//using Prometheus;

namespace BanchoMultiplayerBot.Data;

/// <summary>
/// All statistics available through the metric endpoint if enabled. All statistics provided by the bot is anonymous.
/// </summary>
public class BotStatistics
{
 /*   
    public Gauge IsConnected { get; } = Metrics.CreateGauge("IsConnected", "If the bot is connected to Bancho.");
    
    public Counter MessagesSent { get; } = Metrics.CreateCounter("MessagesSent", "Number of messages sent");
    public Counter MessagesReceived { get; } = Metrics.CreateCounter("MessagesReceived", "Number of messages received");
    public Gauge MessageSendQueue { get; } = Metrics.CreateGauge("MessageQueue", "Numbers of messages currently in queue to be sent.");
    
    public Counter ApiRequests { get; } = Metrics.CreateCounter("TotalApiRequests", "Total amount of API requests.");
    public Counter ApiErrors { get; } = Metrics.CreateCounter("TotalApiErrors", "Total amount of API errors.");
    public Histogram ApiRequestTime { get; } = Metrics.CreateHistogram("ApiRequestTime", "Time for each API request.");

    public Gauge Players { get; } = Metrics.CreateGauge("Players", "Total amount of players in each lobby", "Lobby");
    public Gauge UniquePlayers { get; } = Metrics.CreateGauge("UniquePlayers", "Amount of unique players per hour", "Lobby");
    public Gauge MapViolations { get; } = Metrics.CreateGauge("MapViolations", "Total amount of map violations in each lobby", "Lobby");
    public Gauge GamesPlayed { get; } = Metrics.CreateGauge("GamesPlayed", "Total amount of games played in each lobby", "Lobby");
    public Gauge GamesAborted { get; } = Metrics.CreateGauge("GamesAborted", "Total amount of games aborted in each lobby", "Lobby");
    public Gauge HostSkipCount { get; } = Metrics.CreateGauge("HostSkipCount", "Total amount of host skips in each lobby", "Lobby");
    public Histogram MapLength { get; } = Metrics.CreateHistogram("MapLength", "Length of the last map played", "Lobby");
    public Histogram MapPickTime { get; } = Metrics.CreateHistogram("MapPickTime", "Time it took to pick a map", "Lobby");
    public Histogram MapPlayTime { get; } = Metrics.CreateHistogram("MapPlayTime", "Time it took to start the match", "Lobby");
 */
}