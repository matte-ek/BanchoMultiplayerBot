using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Multiplayer;
using osu.NET.Enums;
using osu.NET.Models.Scores;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors;

public class DataProviderBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();
    private readonly BehaviorDataProvider<DataProviderData> _dataProvider = new(context.Lobby);
    private DataProviderData Data => _dataProvider.Data;
    
    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted()
    {
        Data.LastPlayedBeatmapInfo = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby).Data.BeatmapInfo;
    }

    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        context.Lobby.TimerProvider?.FindOrCreateTimer("MatchLateFinishTimer").Start(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Handles post-match stuff like grabbing player scores and leaderboard scores
    /// for the just played map.
    /// </summary>
    [BotEvent(BotEventType.TimerElapsed, "MatchLateFinishTimer")]
    public void OnMatchLateFinishTimer()
    {
        if (Data.LastPlayedBeatmapInfo == null)
        {
            return;
        }
        
        // We don't want to block other FunCommandsBehavior events with this task
        Task.Run(async () =>
        {
            var recentScores = await GetPlayersRecentLobbyScores();
            var leaderboardScores = await GetLeaderboardScores();

            await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("MatchPlayerScoreData", new MatchPlayerScoreData(recentScores, leaderboardScores));
        });
    }

    private async Task<Score[]> GetLeaderboardScores()
    {
        var leaderboardScoresResult = await context.UsingApiClient(
            async apiClient => await apiClient.GetBeatmapScoresAsync(
                Data.LastPlayedBeatmapInfo!.Id,
                legacyOnly: true,
                Ruleset.Osu
                )
            );
        
        if (leaderboardScoresResult.IsFailure)
        {
            Log.Error("{Component}: API leaderboard lookup failed for beatmap id {BeatmapId}, {Error}",
                nameof(DataProviderBehavior),
                Data.LastPlayedBeatmapInfo!.Id,
                leaderboardScoresResult.Error);
            
            return [];
        }

        var leaderboardScores = leaderboardScoresResult.Value!;

        if (leaderboardScores.Length == 0)
        {
            Log.Warning("{Component}: API leaderboard lookup returned 0 scores using beatmap id {BeatmapId}",
                nameof(DataProviderBehavior),
                Data.LastPlayedBeatmapInfo!.Id);
            
            return [];
        }

        return leaderboardScores;
    }
    
    private async Task<IReadOnlyList<PlayerScoreResult>> GetPlayersRecentLobbyScores()
    {
        var players = context.MultiplayerLobby.Players
            .Where(x => x is { Id: not null, Score: > 0 })
            .ToList();

        var grabScoreTasks = players
                .Select((player, i) => GetPlayerRecentScore(player, TimeSpan.FromMilliseconds(i * 250)))
                .ToList();

        var scores = await Task.WhenAll(grabScoreTasks);

        return scores
            .Where(x => x.Score?.BeatmapId == Data.LastPlayedBeatmapInfo!.Id)
            .ToList();
    }
    
    private async Task<PlayerScoreResult> GetPlayerRecentScore(MultiplayerPlayer player, TimeSpan? executeDelay)
    {
        if (executeDelay != null)
        {
            await Task.Delay(executeDelay.Value);
        }
        
        Log.Information("{Component}: Executing osu! API call for {Name}", nameof(DataProviderBehavior), player);
        
        var scores = await context.UsingApiClient(async (apiClient) =>
            await apiClient.GetUserScoresAsync(
                player.Id!.Value,
                UserScoreType.Recent,
                legacyOnly: true,
                includeFails: true,
                Ruleset.Osu,
                limit: 1,
                offset: 0
                )
            );
        
        return new PlayerScoreResult(player, scores.Value?.FirstOrDefault());
    }
}