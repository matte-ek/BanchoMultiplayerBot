using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using Prometheus;

namespace BanchoMultiplayerBot.Behaviors;

public class StatisticsBehavior(BehaviorEventContext context) : IBehavior
{
    private static readonly Gauge PlayerCount = Metrics.CreateGauge("bot_lobby_player_count", "The number of players in the lobby", "lobby_index");
    private static readonly Counter MatchCount = Metrics.CreateCounter("bot_lobby_match_count", "The number of total matches played", "lobby_index");
    private static readonly Counter MatchAbortedCount = Metrics.CreateCounter("bot_lobby_match_aborted_count", "The number of total matches aborted", "lobby_index");
    
    [BanchoEvent(BanchoEventType.PlayerJoined)]
    public void OnPlayerJoined() => PlayerCount.Set(context.MultiplayerLobby.Players.Count);
    
    [BanchoEvent(BanchoEventType.PlayerDisconnected)]
    public void OnPlayerDisconnected() => PlayerCount.Set(context.MultiplayerLobby.Players.Count);
    
    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted() => MatchCount.Inc();
    
    [BanchoEvent(BanchoEventType.MatchAborted)]
    public void OnMatchAborted() => MatchAbortedCount.Inc();
}