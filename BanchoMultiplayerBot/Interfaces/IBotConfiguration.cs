using BanchoMultiplayerBot.Bancho.Data;

namespace BanchoMultiplayerBot.Interfaces;

public interface IBotConfiguration
{
    public BanchoClientConfiguration BanchoClientConfiguration { get; init; }

    public string OsuApiKey { get; init; }
}