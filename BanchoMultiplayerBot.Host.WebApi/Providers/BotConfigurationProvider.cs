using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Host.WebApi.Providers;

public class BotConfigurationProvider(IConfiguration configuration) : IBotConfiguration
{
    public BanchoClientConfiguration BanchoClientConfiguration { get; init; } = new()
    {
        Username = configuration["Osu:Username"]!,
        Password = configuration["Osu:Password"]!,
        MessageRateLimitCount = configuration.GetValue("Bot:MessageRateLimitCount", 8),
        MessageRateLimitWindow = configuration.GetValue("Bot:MessageRateLimitWindow", 6),
        BanchoReconnectDelay = configuration.GetValue("Bot:BanchoReconnectDelay", 30),
        BanchoReconnectAttempts = configuration.GetValue("Bot:BanchoReconnectAttempts", 5),
        BanchoReconnectAttemptDelay = configuration.GetValue("Bot:BanchoReconnectAttemptDelay", 10),
        BanchoCommandTimeout = configuration.GetValue("Bot:BanchoCommandTimeout", 5),
        BanchoCommandAttempts = configuration.GetValue("Bot:BanchoCommandAttempts", 5)
    };

    public string OsuApiClientId { get; init; } = configuration["Osu:ClientId"]!;
    public string OsuApiClientSecret { get; init; } = configuration["Osu:ClientSecret"]!;

    public string? DiscordWebhookUrl { get; init; } = configuration["Bot:DiscordWebhookUrl"];
}