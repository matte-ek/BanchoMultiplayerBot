using Prometheus;

namespace BanchoMultiplayerBot.Data;

public class BotMetrics
{
    public static readonly Counter ApiRequestsCount = Metrics.CreateCounter("bot_api_requests_count", "The total number of API requests");
    public static readonly Counter ApiRequestsFailedCount = Metrics.CreateCounter("bot_api_requests_failed_count", "The total number of failed API requests");
    public static readonly Histogram ApiRequestsTime = Metrics.CreateHistogram("bot_api_request_time_ms", "Length of a API request in");
}