using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Osu.Data;
using BanchoMultiplayerBot.Providers;
using Microsoft.EntityFrameworkCore;
using osu.NET.Models.Scores;

namespace BanchoMultiplayerBot.Behaviors;

/// <summary>
/// A behavior which will attempt to calculate a new star rating range for the lobby
/// depending on the players' performance in the lobby. This initial version is very
/// primitive to test how things might work.
/// </summary>
public class DynamicRoomBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();
    private readonly BehaviorDataProvider<DynamicRoomBehaviorData> _dataProvider = new(context.Lobby);
    private DynamicRoomBehaviorData Data => _dataProvider.Data;

    [BanchoEvent(BanchoEventType.MatchFinished)]
    public async Task OnMatchFinished()
    {
        await ApplyRoomUpdates();
    }
    
    [BotEvent(BotEventType.BehaviourEvent, "MatchPlayerScoreData")]
    public async Task OnMatchPlayerScoreData(MatchPlayerScoreData data)
    {
        var lastPlayedBeatmapInfo = new BehaviorDataProvider<DataProviderData>(context.Lobby).Data.LastPlayedBeatmapInfo;
        var mapPerformancePointsInfo = await context.Lobby.Bot.PerformancePointService!.CalculatePerformancePoints(lastPlayedBeatmapInfo!.Id);
        var mapDifficultyFactor = ComputeMapDifficultyFactor(data);

        if (mapPerformancePointsInfo == null)
        {
            return;
        }

        var playerSuccessFactorTasks = data.RecentPlayerScores
            .Where(x => x.Score != null)
            .Select(x => ComputePlayerSuccessFactor(x.Score!, lastPlayedBeatmapInfo, mapPerformancePointsInfo))
            .ToList();
        
        var playerSuccessFactorResults = await Task.WhenAll(playerSuccessFactorTasks);
        
        var averageSuccess = playerSuccessFactorResults
            .Where(x => x >= 0)
            .Average();

        if (float.IsNaN(averageSuccess))
        {
            averageSuccess = 0.5f;
        }

        var srChange = 3f * averageSuccess - 2f;

        srChange *= mapDifficultyFactor;
        
        Data.StarRatingTarget += srChange;
        Data.StarRatingTarget = Math.Clamp(Data.StarRatingTarget, 1f, 8f);
        
        Data.HasPendingUpdate = true;
    }

    private static float ComputeMapDifficultyFactor(MatchPlayerScoreData data)
    {
        var scoreCount = data.LeaderboardScores.Length;

        if (scoreCount == 0)
        {
            // This could be a graveyard map or similar, as such
            // we'll have to assume this map is not too difficult.
            return 1f;
        }

        var fcRatio = data.LeaderboardScores.Count(x => x.IsPerfectComboLegacy) / (float)scoreCount;
        var accFactor = data.LeaderboardScores.Average(x => x.Accuracy) / 100f;
        
        var playerScoresAvailableAdd = MathF.Exp(0.1f * (scoreCount - 50f));
        var difficulty = fcRatio + (1f - playerScoresAvailableAdd);
        
        return Math.Clamp(difficulty, 0.0f, 1.0f);
    }

    private async Task<float> ComputePlayerSuccessFactor(
        Score score,
        BeatmapInfo lastPlayedBeatmapInfo,
        BeatmapPerformanceInfo beatmapPerformanceInfo)
    {
        // We'll basically just use pure performance points for this, it's not ideal,
        // but it will hopefully work enough.
        var performancePoints = await context.Lobby.Bot.PerformancePointService!.CalculateScorePerformancePoints(lastPlayedBeatmapInfo.Id, score);

        if (performancePoints == null)
        {
            return -1f;
        }

        return performancePoints.PerformancePoints / beatmapPerformanceInfo.Performance100;
    }

    private async Task ApplyRoomUpdates()
    {
        if (!Data.HasPendingUpdate)
        {
            return;
        }
        
        await using var dbContext = new BotDbContext();
        var roomMapConfiguration = new BehaviorConfigProvider<MapManagerBehaviorConfig>(context.Lobby);
        
        roomMapConfiguration.Data.MaximumStarRating = Data.StarRatingTarget + 1f;
        roomMapConfiguration.Data.MinimumStarRating = Data.StarRatingTarget - 1f;

        var configuration = await dbContext.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == context.Lobby.LobbyConfigurationId);
        if (configuration != null)
        {
            configuration.Name = $"{roomMapConfiguration.Data.MinimumStarRating:.0#}* - {roomMapConfiguration.Data.MaximumStarRating:.0#}** Dynamic SR | Auto Host Rotate";
        }
        
        await roomMapConfiguration.SaveData();
        
        context.SendMessage($"The star rating limit has now been updated to {roomMapConfiguration.Data.MinimumStarRating:.0#} - {roomMapConfiguration.Data.MaximumStarRating:.0#}!");

        Data.HasPendingUpdate = false;
    }
}