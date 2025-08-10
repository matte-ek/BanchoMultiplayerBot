using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;

namespace BanchoMultiplayerBot.Behaviors;

public class DynamicRoomBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();
    
    private readonly BehaviorDataProvider<DynamicRoomBehaviorData> _dataProvider = new(context.Lobby);
    
    private DynamicRoomBehaviorData Data => _dataProvider.Data;

    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        ApplyRoomUpdates();
    }
    
    [BotEvent(BotEventType.BehaviourEvent, "MatchPlayerScoreData")]
    public void OnMatchPlayerScoreData(MatchPlayerScoreData data)
    {
        var mapDifficultyFactor = ComputeMapDifficultyFactor(data);
    }

    private static float ComputeMapDifficultyFactor(MatchPlayerScoreData data)
    {
        var scoreCount = data.LeaderboardScores.Length;

        if (scoreCount == 0)
        {
            return 0.5f;
        }

        var fcRatio = data.LeaderboardScores.Count(x => x.IsPerfectComboLegacy) / (float)scoreCount;
        var playerScoresAvailableFactor = MathF.Exp(0.1f * (scoreCount - 50f));
        var difficulty = fcRatio * playerScoresAvailableFactor;
        
        return Math.Clamp(difficulty, 0.0f, 1.0f);
    }

    private void ApplyRoomUpdates()
    {
        if (!Data.HasPendingUpdate)
        {
            return;
        }
        
        var roomConfiguration = new BehaviorConfigProvider<MapManagerBehaviorConfig>(context.Lobby);
        
        roomConfiguration.Data.MaximumStarRating = Data.StarRatingMaximum;
        roomConfiguration.Data.MinimumStarRating = Data.StarRatingMinimum;
        
        context.SendMessage($"The star rating limit has now been updated to {roomConfiguration.Data.MinimumStarRating:.0#} - {roomConfiguration.Data.MaximumStarRating:.0#}!");
    }
}