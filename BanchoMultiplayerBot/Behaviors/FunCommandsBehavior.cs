using System.Globalization;
using System.Text;
using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Osu.Extensions;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Multiplayer;
using Humanizer;
using OsuSharp.Enums;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors;

public class FunCommandsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();

    private readonly BehaviorDataProvider<FunCommandsBehaviorData> _dataProvider = new(context.Lobby);
    private readonly BehaviorConfigProvider<FunCommandsBehaviorConfig> _configProvider = new(context.Lobby);

    private FunCommandsBehaviorData Data => _dataProvider.Data;
    private FunCommandsBehaviorConfig Config => _configProvider.Data;

    [BotEvent(BotEventType.CommandExecuted, "PlayTime")]
    public void OnPlayTimeCommandExecuted(CommandEventContext commandEventContext)
    {
        var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == commandEventContext.Player?.Name);
        var totalPlaytime = TimeSpan.FromSeconds(commandEventContext.User.Playtime);
        var currentPlaytime = TimeSpan.FromSeconds(0);
    
        // Append current playtime to the record
        if (record != null)
        {
            currentPlaytime = DateTime.UtcNow - record.JoinTime;
            totalPlaytime += currentPlaytime;
        }
        
        commandEventContext.Reply($"{commandEventContext.Player?.Name} has been here for {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} ({totalPlaytime.TotalHours:F0}h) in total).");
    }

    [BotEvent(BotEventType.CommandExecuted, "PlayStatistics")]
    public void OnPlayStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        commandEventContext.Reply($"{commandEventContext.Player?.Name} has played {commandEventContext.User.MatchesPlayed} matches with a total of {commandEventContext.User.NumberOneResults} #1's.");
    }

    [BotEvent(BotEventType.CommandExecuted, "PersonalMapStatistics")]
    public async Task OnPersonalMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Player?.Id == null)
        {
            return;
        }
        
        using var scoreRepository = new ScoreRepository();
        
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var scores = await scoreRepository.GetScoresByMapAndPlayerId(commandEventContext.Player.Id.Value, mapManagerDataProvider.Data.BeatmapInfo.Id);
        
        var failCount = scores.Count(x => x.Rank == 1);
        var passCount = scores.Count - failCount;
        var passFail =  failCount >= passCount ? "fail" : "pass";
        var mostCommonRank = scores.Select(x => x.Rank)
            .GroupBy(i=>i)
            .OrderByDescending(grp=>grp.Count())
            .Select(grp=>grp.Key).FirstOrDefault();
        var avgAccuracy = scores.Count > 0 ? scores.Average(x => x.GetAccuracy()) : 0;

        if (scores.Count > 0)
        {
            commandEventContext.Reply(passFail == "fail"
                ? $"{commandEventContext.Player?.Name}, you've played this map {scores.Count} times in this lobby! You usually {passFail} the map! :("
                : $"{commandEventContext.Player?.Name}, you've played this map {scores.Count} times in this lobby! You usually {passFail} the map with an {Score.GetRankString(mostCommonRank!)} rank, average accuracy: {avgAccuracy:0.00}%!");
        }
        else
        {
            commandEventContext.Reply($"{commandEventContext.Player?.Name}, you haven't played this map in this lobby yet!");  
        }
    }
    
    [BotEvent(BotEventType.CommandExecuted, "BestMapStatistics")]
    public async Task OnBestMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Player?.Id == null)
        {
            return;
        }
        
        using var scoreRepository = new ScoreRepository();
        
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var scores = await scoreRepository.GetScoresByMapAndPlayerId(commandEventContext.Player.Id.Value, mapManagerDataProvider.Data.BeatmapInfo.Id);
        var bestScore = scores.MaxBy(x => x.TotalScore);
        var bestScoreAcc = bestScore?.GetAccuracy();
                    
        commandEventContext.Reply(bestScore != null
            ? $"{commandEventContext.Player?.Name}, your best score on this map in this lobby is an {bestScore.GetRankString()} rank with {bestScoreAcc:0.00}% accuracy and x{bestScore.MaxCombo} combo, {bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss}!"
            : $"{commandEventContext.Player?.Name}, you haven't played this map in this lobby yet!");
    }
    
    [BotEvent(BotEventType.CommandExecuted, "MapStatistics")]
    public async Task OnMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        using var gameRepository = new GameRepository();
        using var scoreRepository = new ScoreRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var beatmapId = mapManagerDataProvider.Data.BeatmapInfo.Id;
        
        var totalPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, null);
        var pastWeekPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, DateTime.UtcNow.AddDays(-7));

        var recentGames = await gameRepository.GetRecentGames(beatmapId, 50);
        var recentScores = await scoreRepository.GetScoresByMapId(beatmapId, 50);

        var mapPosition = await scoreRepository.GetMapPlayCountByLobbyId(context.Lobby.LobbyConfigurationId - 1, beatmapId);

        var outputMessage = new StringBuilder();

        outputMessage.Append($"This map has been played {totalPlayCount} times!");

        if (totalPlayCount != 0)
        {
            outputMessage.Append($" ({pastWeekPlayCount} times past week)");
        }

        if (mapPosition != null)
        {
            outputMessage.Append($" | #{mapPosition} most played");
        }

        if (recentScores.Any())
        {
            var avgAccuracy = recentScores.Average(x => x.GetAccuracy());
      
            outputMessage.Append($" | Average accuracy: {avgAccuracy:0.00}%");
        }

        if (recentGames.Any())
        {
            List<float> leaveRatio = [];
            List<float> passRatio = [];

            // Calculate ratios for the last 10 games
            foreach (var game in recentGames)
            {
                if (game.PlayerPassedCount == -1)
                {
                    continue;
                }

                leaveRatio.Add((float)game.PlayerFinishCount / game.PlayerCount);
                passRatio.Add(game.PlayerFinishCount == 0
                    ? 0f
                    : (float)game.PlayerPassedCount / game.PlayerFinishCount);
            }

            if (passRatio.Count != 0)
            {
                var avgLeavePercentage = 100f - MathF.Min(leaveRatio.Average() * 100f, 100f);
                var avgPassPercentage = MathF.Min(passRatio.Average() * 100f, 100f);

                outputMessage.Append($" | {avgLeavePercentage:0}% leave the lobby and {avgPassPercentage:0}% pass!");
            }
        }

        commandEventContext.Reply(outputMessage.ToString());
    }

    [BotEvent(BotEventType.CommandExecuted, "LastPlayed")]
    public async Task OnLastPlayedCommandExecuted(CommandEventContext commandEventContext)
    {
        using var gameRepository = new GameRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var beatmapInfo = mapManagerDataProvider.Data.BeatmapInfo;
        var recentGames = await gameRepository.GetRecentGames(beatmapInfo.Id, 1);

        if (recentGames.Count == 0)
        {
            commandEventContext.Reply("This map has not been played yet!");
            return;
        }
        
        commandEventContext.Reply($"The map was last played {recentGames[0].Time.Humanize(utcDate: false, culture: CultureInfo.InvariantCulture)}!");
    }

    [BotEvent(BotEventType.CommandExecuted, "PerformancePoints")]
    public async Task OnPerformancePointsCommandExecuted(CommandEventContext commandEventContext)
    {
        if (context.Lobby.Bot.PerformancePointCalculator == null)
        {
            commandEventContext.Reply("Performance point calculator is not available.");
            return;
        }
        
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var beatmapId = mapManagerDataProvider.Data.BeatmapInfo.Id;
        int mods = 0;
        
        if (commandEventContext.Arguments.Length > 0)
        {
            mods = ScoreExtensions.GetModsBitset(commandEventContext.Arguments[0].Chunk(2).Select(x => new string(x)).ToArray());
        }
        
        var ppInfo = await context.Lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(beatmapId, mods);
        
        commandEventContext.Reply(ppInfo != null
            ? $"{commandEventContext.Message.Sender}, 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp"
            : "Error calculating performance points");
    }

    [BanchoEvent(BanchoEventType.PlayerJoined)]
    public void OnPlayerJoined(MultiplayerPlayer player)
    {
        Data.PlayerTimeRecords.Add(new FunCommandsBehaviorData.PlayerTimeRecord
        {
            PlayerName = player.Name,
            JoinTime = DateTime.UtcNow
        });
    }
    
    [BanchoEvent(BanchoEventType.PlayerDisconnected)]
    public async Task OnPlayerDisconnected(MultiplayerPlayer player)
    {
        using var userRepository = new UserRepository();

        var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == player.Name);
        if (record == null)
        {
            return;
        }

        var user = await userRepository.FindOrCreateUser(player.Name);
        
        user.Playtime += (int)(DateTime.UtcNow - record.JoinTime).TotalSeconds;

        await userRepository.Save();
        
        Data.PlayerTimeRecords.Remove(record);
    }

    [BanchoEvent(BanchoEventType.SettingsUpdated)]
    public void OnSettingsUpdated()
    {
        // Remove records of players that are no longer in the lobby
        foreach (var record in Data.PlayerTimeRecords.ToList().Where(record => context.MultiplayerLobby.Players.All(x => x.Name != record.PlayerName)))
        {
            Data.PlayerTimeRecords.Remove(record);
        }
    }

    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted()
    {
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);

        Data.MapStartPlayerCount = context.MultiplayerLobby.Players.Count;
        Data.LastPlayedBeatmapInfo = mapManagerDataProvider.Data.BeatmapInfo;
    }

    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        context.Lobby.TimerProvider?.FindOrCreateTimer("MatchLateFinishTimer").Start(TimeSpan.FromSeconds(10));
    }

    [BotEvent(BotEventType.TimerElapsed, "MatchLateFinishTimer")]
    public async Task OnMatchLateFinishTimer()
    {
        var recentScores = await GetRecentScores();

        await StoreGameData(recentScores);
        await StorePlayerFinishData(recentScores);
        await AnnounceLeaderboardResults(recentScores);
    }

    private async Task StoreGameData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (Data.LastPlayedBeatmapInfo == null)
        {
            return;
        }

        using var gameRepository = new GameRepository();

        var playerFinishCount = recentScores.Count;
        var playerPassedCount = recentScores.Count(x => x.Score?.Grade != Grade.F);

        var game = new Game
        {
            BeatmapId = Data.LastPlayedBeatmapInfo.Id,
            Time = DateTime.UtcNow,
            PlayerCount = Data.MapStartPlayerCount,
            PlayerFinishCount = playerFinishCount,
            PlayerPassedCount = playerPassedCount
        };

        await gameRepository.AddGame(game);

        await StoreScoreData(recentScores, game);
    }

    private async Task StorePlayerFinishData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        using var userRepository = new UserRepository();

        var highestScorePlayer = recentScores.MaxBy(x => x.Player.Score);
        if (context.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
        {
            var user = await userRepository.FindUser(highestScorePlayer.Player.Name) ??
                       await userRepository.CreateUser(highestScorePlayer.Player.Name);

            user.NumberOneResults++;
        }

        foreach (var result in recentScores)
        {
            var user = await userRepository.FindUser(result.Player.Name) ??
                       await userRepository.CreateUser(result.Player.Name);

            if (result.Score?.Grade != Grade.F)
                user.MatchesPlayed++;
        }

        await userRepository.Save();
    }

    private async Task AnnounceLeaderboardResults(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (Data.LastPlayedBeatmapInfo == null)
        {
            return;
        }
        
        var leaderboardScores = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapScoresAsync(Data.LastPlayedBeatmapInfo.Id, Ruleset.Osu, legacyOnly: true));
        if (leaderboardScores == null || leaderboardScores.Length == 0)
        {
            return;
        }
        
        foreach (var score in recentScores)
        {
            var leaderboardScore = leaderboardScores.FirstOrDefault(x => x.Id == score.Score?.Id);
            if (leaderboardScore == null)
            {
                continue;
            }
            
            var scorePosition = leaderboardScores.ToList().FindIndex(x => x.Id == score.Score?.Id);
            if (scorePosition == -1)
            {
                continue;
            }

            Log.Verbose("FunCommandsBehavior: Found leaderboard score with id {ScoreId} and placement {Placement}", score.Score?.Id, scorePosition + 1);

            if (Config.AnnounceLeaderboardScores)
            {
                context.SendMessage($"Congratulations {score.Player.Name} for getting #{scorePosition + 1} on the map's leaderboard!");
            }
        }
    }

    private async Task StoreScoreData(IReadOnlyList<PlayerScoreResult> recentScores, Game game)
    {
        using var userRepository = new UserRepository();
        using var scoreRepository = new ScoreRepository();

        try
        {
            foreach (var result in recentScores)
            {
                if (result.Score?.Statistics.Count50 == null)
                {
                    continue;
                }

                var score = result.Score;
                var user = await userRepository.FindOrCreateUser(result.Player.Name);
                
                await scoreRepository.Add(new Score
                {
                    UserId = user.Id,
                    PlayerId = result.Player.Id,
                    LobbyId = context.Lobby.LobbyConfigurationId - 1,
                    GameId = game.Id,
                    OsuScoreId = score.Id,
                    BeatmapId = score.Beatmap!.Id,
                    TotalScore = score.TotalScore,
                    Rank = score.Grade.GetRankId(),
                    MaxCombo = score.MaxCombo,
                    Count300 = score.Statistics.Count300 ?? 0,
                    Count100 = score.Statistics.Count100 ?? 0,
                    Count50 = score.Statistics.Count50 ?? 0,
                    CountMiss = score.Statistics.Misses,
                    Mods = score.GetModsBitset()
                });
            }
            
            Log.Verbose($"FunCommands: Stored {recentScores.Count} scores for game {game.Id}");
        }
        catch (Exception e)
        {
            Log.Error($"Exception at StoreScoreData(): {e}");
        }

        await scoreRepository.Save();
    }

    /// <summary>
    /// Find all scores for the players in the lobby, if they have played the last played map.
    /// </summary>
    private async Task<IReadOnlyList<PlayerScoreResult>> GetRecentScores()
    {
        var players = context.MultiplayerLobby.Players.Where(x => x.Id != null && x.Score > 0).ToList();
        var grabScoreTasks = new List<Task<OsuSharp.Models.Scores.Score[]?>>();

        await context.Lobby.Bot.OsuApiClient.EnsureAccessTokenAsync();
        
        for (int i = 0; i < players.Count;i++)
        {
            int index = i;
            
            grabScoreTasks.Add(Task.Run(async () =>
            {
                await Task.Delay(index * 250);
                
                Log.Information($"FunCommandsBehavior: Executing osu! API call for {players[index].Name}");
                
                return await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(players[index].Id!.Value, UserScoreType.Recent, true, true, Ruleset.Osu, 1));
            }));
        }
        
        await Task.WhenAll(grabScoreTasks);
        
        return players.Select(player => new PlayerScoreResult(player, grabScoreTasks.Select(x => x.Result?.FirstOrDefault()).ToList()
                .FirstOrDefault(x => x?.UserId == player.Id!)))
                .ToList();
    }
}