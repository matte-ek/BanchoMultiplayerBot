using BanchoMultiplayerBot.Interfaces;
using Prometheus;

namespace BanchoMultiplayerBot.Behaviors;

public class StatisticsBehavior : IBehavior
{
    private static readonly Gauge PlayerCount = Metrics.CreateGauge("bot_lobby_player_count", "The number of players in the lobby", "lobby_index");
    private static readonly Gauge UniquePlayers = Metrics.CreateGauge("bot_lobby_player_count", "The number of players in the lobby", "lobby_index");
}