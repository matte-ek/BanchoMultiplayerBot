using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Host.WebApi.Providers;

public class BotConfigurationProvider(IConfiguration configuration) : IBotConfiguration
{
    public BanchoClientConfiguration BanchoClientConfiguration { get; init; } = new()
    {
        Username = configuration["Osu:Username"]!,
        Password = configuration["Osu:Password"]!,
        IsBotAccount = configuration["Osu:BotAccount"] == "true"
    };

    public string OsuApiKey { get; init; } = configuration["Osu:ApiKey"]!;
}