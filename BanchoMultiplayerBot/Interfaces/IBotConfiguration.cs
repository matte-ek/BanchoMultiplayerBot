using BanchoMultiplayerBot.Bancho.Data;

namespace BanchoMultiplayerBot.Interfaces;

public interface IBotConfiguration
{
    public BanchoClientConfiguration BanchoClientConfiguration { get; init; }

    public string OsuApiClientId { get; init; }
    public string OsuApiClientSecret { get; init; }
    
    public string? DiscordWebhookUrl { get; init; }
    
    public string? PerformancePointServiceUrl { get; init; }
    public string? BeatmapCacheDirectory { get; init; }
}