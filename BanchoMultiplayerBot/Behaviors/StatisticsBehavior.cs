using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;
using Prometheus;

namespace BanchoMultiplayerBot.Behaviors;

public class StatisticsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    private readonly BehaviorDataProvider<StatisticsBehaviorData> _dataProvider = new(context.Lobby);
    private StatisticsBehaviorData Data => _dataProvider.Data;
 
    public async Task SaveData() => await _dataProvider.SaveData();
    
    private static readonly Gauge PlayerCount = Metrics.CreateGauge("bot_lobby_player_count", "The number of players in the lobby", "lobby_index");
    
    private static readonly Counter MatchCount = Metrics.CreateCounter("bot_lobby_match_count", "The number of total matches played", "lobby_index");
    private static readonly Counter MatchAbortedCount = Metrics.CreateCounter("bot_lobby_match_aborted_count", "The number of total matches aborted", "lobby_index");
    
    private static readonly Histogram MapLength = Metrics.CreateHistogram("bot_lobby_map_length", "Length of the maps played", "lobby_index");
    private static readonly Histogram MapPickTime = Metrics.CreateHistogram("bot_lobby_map_pick_time", "Time it took to pick a map", "lobby_index");
    private static readonly Histogram MapIntermissionTime = Metrics.CreateHistogram("bot_lobby_map_intermission_time", "Time in lobby between maps", "lobby_index");
    
    private static readonly Counter HostSkipCount = Metrics.CreateCounter("bot_lobby_host_skip_count", "The number of times a host got skipped", "lobby_index");
    private static readonly Counter HostViolationCount = Metrics.CreateCounter("bot_lobby_host_violation_count", "The number of host violations", "lobby_index");
    
    [BanchoEvent(BanchoEventType.PlayerJoined)]
    public void OnPlayerJoined() => PlayerCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Set(context.MultiplayerLobby.Players.Count);
    
    [BanchoEvent(BanchoEventType.PlayerDisconnected)]
    public void OnPlayerDisconnected() => PlayerCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Set(context.MultiplayerLobby.Players.Count);

    [BanchoEvent(BanchoEventType.MatchAborted)]
    public void OnMatchAborted()
    {
        MatchAbortedCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Inc();
        Data.MatchFinishedTime = DateTime.UtcNow;
    }
    
    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);

        Data.MatchFinishedTime = DateTime.UtcNow;
     
        MapLength.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Observe(mapManagerDataProvider.Data.BeatmapInfo.Length.TotalSeconds);
    }
    
    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted()
    {
        MatchCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Inc();
        
        MapIntermissionTime.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Observe((DateTime.UtcNow - Data.MatchFinishedTime).TotalSeconds);
    }
    
    [BotEvent(BotEventType.BehaviourEvent, "MapManagerNewMap")]
    public void OnNewMapSelected()
    {
        MapPickTime.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Observe((DateTime.UtcNow - Data.MatchFinishedTime).TotalSeconds);
    }
    
    [BotEvent(BotEventType.BehaviourEvent, "MapManagerInvalidMap")]
    public void OnInvalidMapPicked()
    {
        HostViolationCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Inc();
    }
    
    [BotEvent(BotEventType.BehaviourEvent, "HostQueueHostSkipped")]
    public void OnHostSkipped()
    {
        HostSkipCount.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).Inc();
    }
}