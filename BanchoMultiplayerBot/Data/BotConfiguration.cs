using BanchoMultiplayerBot.Bancho.Data;

namespace BanchoMultiplayerBot.Data;

public class BotConfiguration
{

    public BanchoClientConfiguration BanchoClientConfiguration { get; init; } = null!;

    public string OsuApiKey { get; init; } = null!;
}