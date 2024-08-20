using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Data;

public class BotConfiguration : IBotConfiguration
{
    public BanchoClientConfiguration BanchoClientConfiguration { get; init; } = null!;

    public string OsuApiClientId { get; init; } = null!;
    
    public string OsuApiClientSecret { get; init; } = null!;
}