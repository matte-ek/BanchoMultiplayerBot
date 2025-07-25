using System.Globalization;
using System.Text;
using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Osu.Data;
using BanchoMultiplayerBot.Osu.Extensions;
using BanchoMultiplayerBot.Providers;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Multiplayer;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osu.NET.Enums;
using Prometheus;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors;

public class FunCommandsBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
{
    public async Task SaveData() => await _dataProvider.SaveData();

    private readonly BehaviorDataProvider<FunCommandsBehaviorData> _dataProvider = new(context.Lobby);
    private readonly BehaviorConfigProvider<FunCommandsBehaviorConfig> _configProvider = new(context.Lobby);

    private FunCommandsBehaviorData Data => _dataProvider.Data;
    private FunCommandsBehaviorConfig Config => _configProvider.Data;

    private static readonly Histogram StoreGameDataTime = Metrics.CreateHistogram(
        "bot_behavior_store_game_data_time", 
        "Time it took to store game related data to the database", 
        "lobby_index");

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

        commandEventContext.Reply($"{commandEventContext.Player?.Name} has been here for {currentPlaytime.Humanize(3, maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second)}, playing {"match".ToQuantity(record?.MatchedPlayerCount ?? 0)}. ({totalPlaytime.Humanize(4, maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second)} ({totalPlaytime.TotalHours:F0}h) in total).");
    }

    [BotEvent(BotEventType.CommandExecuted, "PlayStatistics")]
    public void OnPlayStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        commandEventContext.Reply(
            $"{commandEventContext.Player?.Name} has played {"match".ToQuantity(commandEventContext.User.MatchesPlayed)} with a total of {commandEventContext.User.NumberOneResults} #1's.");
    }

    [BotEvent(BotEventType.CommandExecuted, "PersonalMapStatistics")]
    public async Task OnPersonalMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Player?.Id == null)
        {
            return;
        }

        await using var scoreRepository = new ScoreRepository();
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        
        var scores = await scoreRepository.GetScoresByMapAndPlayerIdAsync(
            commandEventContext.Player.Id.Value,
            mapManagerDataProvider.Data.BeatmapInfo.Id);

        var failCount = scores.Count(x => x.Rank == OsuRank.F);
        var passCount = scores.Count - failCount;
        var passFail = failCount >= passCount ? "fail" : "pass";
        var mostCommonRank = scores.Select(x => x.Rank)
            .GroupBy(i => i)
            .OrderByDescending(grp => grp.Count())
            .Select(grp => grp.Key).FirstOrDefault();
        var avgAccuracy = scores.Count > 0 ? scores.Average(ScoreUtilities.CalculateAccuracy) : 0;

        if (scores.Count > 0)
        {
            commandEventContext.Reply(passFail == "fail"
                ? $"{commandEventContext.Player?.Name}, you've played this map {"time".ToQuantity(scores.Count)} in this lobby! You usually {passFail} the map! :("
                : $"{commandEventContext.Player?.Name}, you've played this map {"time".ToQuantity(scores.Count)} times in this lobby! You usually {passFail} the map with an {mostCommonRank.ToString()} rank, average accuracy: {avgAccuracy:0.00}%!");
        }
        else
        {
            commandEventContext.Reply(
                $"{commandEventContext.Player?.Name}, you haven't played this map in this lobby yet!");
        }
    }

    [BotEvent(BotEventType.CommandExecuted, "BestMapStatistics")]
    public async Task OnBestMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Player?.Id == null)
        {
            return;
        }

        await using var scoreRepository = new ScoreRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var scores = await scoreRepository.GetScoresByMapAndPlayerIdAsync(commandEventContext.Player.Id.Value, mapManagerDataProvider.Data.BeatmapInfo.Id);
        var bestScore = scores.MaxBy(x => x.TotalScore);
        var bestScoreAcc = bestScore != null ? ScoreUtilities.CalculateAccuracy(bestScore) : 0f;
        var stringBuilder = new StringBuilder(64);

        stringBuilder.Append(commandEventContext.Player?.Name);

        if (bestScore != null)
        {
            stringBuilder.Append(", your best score on this map in this lobby is ");

            // Make sure to use "an" or "a" depending on the rank
            // I wish Humanizer would do this for me :(
            stringBuilder.Append(bestScore.Rank is OsuRank.SS or OsuRank.S or OsuRank.A or OsuRank.F ? "an " : "a ");

            stringBuilder.Append($"{bestScore.Rank.ToString()} rank with {bestScoreAcc:0.00}% accuracy and x{bestScore.MaxCombo} combo, {bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss}");

            if (bestScore.Mods != 0)
            {
                stringBuilder.Append(" + " + ((OsuMods)bestScore.Mods).ToAbbreviatedForm());
            }

            stringBuilder.Append(
                $" set {bestScore.Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}");
        }
        else
        {
            stringBuilder.Append(", you haven't played this map in this lobby yet!");
        }

        commandEventContext.Reply(stringBuilder.ToString());
    }

    [BotEvent(BotEventType.CommandExecuted, "MapStatistics")]
    public async Task OnMapStatisticsCommandExecuted(CommandEventContext commandEventContext)
    {
        await using var gameRepository = new GameRepository();
        await using var scoreRepository = new ScoreRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var beatmapId = mapManagerDataProvider.Data.BeatmapInfo.Id;

        var totalPlayCount = await gameRepository.GetGameCountByMapIdAsync(beatmapId, null);
        var pastWeekPlayCount = await gameRepository.GetGameCountByMapIdAsync(beatmapId, DateTime.UtcNow.AddDays(-7));

        var recentGames = await gameRepository.GetRecentGames(beatmapId, 50);
        var recentScores = await scoreRepository.GetScoresByMapIdAsync(beatmapId, 50);

        var mapPosition = await scoreRepository.GetMapPlayCountByLobbyIdAsync(context.Lobby.LobbyConfigurationId - 1, beatmapId);

        var outputMessage = new StringBuilder();

        outputMessage.Append($"This map has been played {"time".ToQuantity(totalPlayCount)}!");

        if (totalPlayCount != 0)
        {
            outputMessage.Append($" ({"time".ToQuantity(pastWeekPlayCount)} past week)");
        }

        if (mapPosition != null)
        {
            outputMessage.Append($" | #{mapPosition} most played");
        }

        if (recentScores.Any())
        {
            var avgAccuracy = recentScores.Average(ScoreUtilities.CalculateAccuracy);

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
        await using var gameRepository = new GameRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var beatmapInfo = mapManagerDataProvider.Data.BeatmapInfo;
        var recentGames = await gameRepository.GetRecentGames(beatmapInfo.Id, 1);

        if (recentGames.Count == 0)
        {
            commandEventContext.Reply("This map has not been played yet!");
            return;
        }

        commandEventContext.Reply($"The map was last played {recentGames[0].Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}!");
    }

    [BotEvent(BotEventType.CommandExecuted, "MapRecord")]
    public async Task OnMapRecordCommandExecuted(CommandEventContext commandEventContext)
    {
        await using var scoreRepository = new ScoreRepository();

        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(context.Lobby);
        var score = await scoreRepository.GetMapBestScore(mapManagerDataProvider.Data.BeatmapInfo.Id);

        if (score == null)
        {
            commandEventContext.Reply("No scores have been set on this map yet!");
            return;
        }

        var accuracy = ScoreUtilities.CalculateAccuracy(score);

        commandEventContext.Reply($"[https://osu.ppy.sh/users/@{score.User.Name.ToIrcNameFormat()} {score.User.Name}] has the best lobby score on this map with an {score.Rank.ToString()} rank, {accuracy:0.00}% accuracy and x{score.MaxCombo} combo, {score.Count300}/{score.Count100}/{score.Count50}/{score.CountMiss}, set {score.Time.Humanize(utcDate: true, culture: CultureInfo.InvariantCulture)}!");
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

    [BotEvent(BotEventType.CommandExecuted, "LeaveCount")]
    public void OnLeaveCountCommandExecuted(CommandEventContext commandEventContext)
    {
        var leftCount = Data.MapStartPlayerCount - Data.MapFinishPlayerCount;

        commandEventContext.Reply($"There were {leftCount} player(s) that left the lobby the previous map!");
    }

    [BotEvent(BotEventType.CommandExecuted, "TeamsMode")]
    public async Task OnTeamsModeCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Arguments.Length == 0 || commandEventContext.Player?.Name == null)
        {
            // This should not happen.
            return;
        }

        await using var botDbContext = new BotDbContext();

        var teamsMode = commandEventContext.Arguments[0].ToLowerInvariant();
        var newTeamsModeState = teamsMode == "on";

        Data.InTeamsMode = newTeamsModeState;
        Data.TeamsModeActivator = commandEventContext.Player.Name;

        // Apply the new teams mode state to the config
        var configuration = await botDbContext.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == context.Lobby.LobbyConfigurationId);

        if (configuration == null)
        {
            return;
        }

        // Ideally we should reset to the previous mode, but we don't have that information,
        // and the effort to account for that is not worth it.
        configuration.TeamMode = newTeamsModeState ? LobbyFormat.TeamVs : LobbyFormat.HeadToHead;

        await botDbContext.SaveChangesAsync();
        await context.ExecuteCommandAsync<MatchSetSettingsCommand>([Data.InTeamsMode ? "2" : "0", "0", "16"]);

        commandEventContext.Reply($"Teams mode has been {(newTeamsModeState ? "enabled" : "disabled")}.");
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
        await using var userRepository = new UserRepository();

        // Restore teams mode if activated
        if (player.Name == Data.TeamsModeActivator && Data.InTeamsMode)
        {
            await using var botDbContext = new BotDbContext();

            var configuration = await botDbContext.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == context.Lobby.LobbyConfigurationId);
            if (configuration == null)
            {
                return;
            }

            Data.InTeamsMode = false;

            configuration.TeamMode = LobbyFormat.HeadToHead;

            await botDbContext.SaveChangesAsync();
        }

        var record = Data.PlayerTimeRecords.FirstOrDefault(x => x.PlayerName == player.Name);
        if (record == null)
        {
            return;
        }

        var user = await userRepository.FindOrCreateUserAsync(player.Name);

        user.Playtime += (int)(DateTime.UtcNow - record.JoinTime).TotalSeconds;

        await userRepository.SaveAsync();

        Data.PlayerTimeRecords.Remove(record);
    }

    [BanchoEvent(BanchoEventType.SettingsUpdated)]
    public void OnSettingsUpdated()
    {
        // Remove records of players that are no longer in the lobby
        foreach (var record in Data.PlayerTimeRecords.ToList()
                     .Where(record => context.MultiplayerLobby.Players.All(x => x.Name != record.PlayerName)))
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

        foreach (var data in Data.PlayerTimeRecords)
        {
            data.MatchedPlayerCount++;
        }
    }

    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        Data.MapFinishPlayerCount = Data.PlayerTimeRecords.Count(x =>
            x.MatchedPlayerCount > 0 &&
            context.MultiplayerLobby.Players.Any(y => y.Name.ToIrcNameFormat() == x.PlayerName.ToIrcNameFormat()));

        context.Lobby.TimerProvider?.FindOrCreateTimer("MatchLateFinishTimer").Start(TimeSpan.FromSeconds(15));
    }

    [BotEvent(BotEventType.TimerElapsed, "MatchLateFinishTimer")]
    public Task OnMatchLateFinishTimer()
    {
        // We don't want to block other FunCommandsBehavior events with this task
        Task.Run(async () =>
        {
            var recentScores = await GetRecentScores();

            using (StoreGameDataTime.WithLabels(context.Lobby.LobbyConfigurationId.ToString()).NewTimer())
            {
                await StoreGameData(recentScores);
                await StorePlayerFinishData(recentScores);
                await AnnounceLeaderboardResults(recentScores);
            }
        });

        return Task.CompletedTask;
    }

    private async Task StoreGameData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (Data.LastPlayedBeatmapInfo == null)
        {
            return;
        }

        await using var gameRepository = new GameRepository();

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

        await gameRepository.AddAsync(game);
        await gameRepository.SaveAsync();

        await StoreScoreData(recentScores, game);
    }

    private async Task StorePlayerFinishData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        await using var userRepository = new UserRepository();

        var highestScorePlayer = recentScores.MaxBy(x => x.Player.Score);
        if (context.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
        {
            var user = await userRepository.FindUserAsync(highestScorePlayer.Player.Name) ??
                       await userRepository.CreateUserAsync(highestScorePlayer.Player.Name);

            user.NumberOneResults++;
        }

        foreach (var result in recentScores)
        {
            var user = await userRepository.FindUserAsync(result.Player.Name) ??
                       await userRepository.CreateUserAsync(result.Player.Name);

            user.MatchesPlayed++;
        }

        await userRepository.SaveAsync();
    }

    private async Task AnnounceLeaderboardResults(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (Data.LastPlayedBeatmapInfo == null)
        {
            return;
        }

        var leaderboardScoresResult = await context.UsingApiClient(async (apiClient) =>
            await apiClient.GetBeatmapScoresAsync(Data.LastPlayedBeatmapInfo.Id, true, Ruleset.Osu));
        if (leaderboardScoresResult.IsFailure)
        {
            Log.Error("{Component}: API leaderboard lookup failed for beatmap id {BeatmapId}, {Error}",
                nameof(FunCommandsBehavior),
                Data.LastPlayedBeatmapInfo.Id,
                leaderboardScoresResult.Error);
            
            return;
        }

        var leaderboardScores = leaderboardScoresResult.Value!;

        if (!leaderboardScores.Any())
        {
            Log.Warning("{Component}: API leaderboard lookup returned 0 scores using beatmap id {BeatmapId}",
                nameof(FunCommandsBehavior),
                Data.LastPlayedBeatmapInfo.Id);
            
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

            Log.Verbose("FunCommandsBehavior: Found leaderboard score with id {ScoreId} and placement {Placement}",
                score.Score?.Id, scorePosition + 1);

            if (Config.AnnounceLeaderboardScores)
            {
                context.SendMessage(
                    $"Congratulations {score.Player.Name} for getting #{scorePosition + 1} on the map's leaderboard!");
            }
        }
    }

    private async Task StoreScoreData(IReadOnlyList<PlayerScoreResult> recentScores, Game game)
    {
        await using var userRepository = new UserRepository();
        await using var scoreRepository = new ScoreRepository();
        
        try
        {
            foreach (var result in recentScores)
            {
                if (result.Score?.Statistics.Meh == null)
                {
                    continue;
                }

                if (result.Score?.BeatmapId != game.BeatmapId)
                {
                    Log.Warning("{Component}: Ignoring score {ScoreId} due to wrong beatmap id. ({ScoreBeatmapId} != {GameBeatmapId})", 
                        nameof(FunCommandsBehavior),
                        result.Score?.Id,
                        result.Score?.BeatmapId,
                        game.BeatmapId);
                    
                    continue;
                }
                
                var score = result.Score;
                var user = await userRepository.FindOrCreateUserAsync(result.Player.Name);

                await scoreRepository.AddAsync(new Score
                {
                    UserId = user.Id,
                    PlayerId = result.Player.Id,
                    LobbyId = context.Lobby.LobbyConfigurationId - 1,
                    GameId = game.Id,
                    OsuScoreId = score.Id,
                    BeatmapId = score.BeatmapId,
                    TotalScore = score.LegacyTotalScore ?? 0,
                    Rank = score.Grade.GetOsuRank(),
                    MaxCombo = score.MaxCombo,
                    Count300 = score.Statistics.Great,
                    Count100 = score.Statistics.Ok,
                    Count50 = score.Statistics.Meh,
                    CountMiss = score.Statistics.Miss,
                    Mods = score.GetModsBitset(),
                    Time = DateTime.UtcNow
                });
            }

            Log.Verbose("{Component}: Stored {ScoreCount} scores for game {GameId}", 
                nameof(FunCommandsBehavior), 
                recentScores.Count,
                game.Id);
        }
        catch (Exception e)
        {
            Log.Error("{Component}: Exception at StoreScoreData(): {Exception}", nameof(FunCommandsBehavior), e);
        }

        await scoreRepository.SaveAsync();
    }

    /// <summary>
    /// Find all scores for the players in the lobby, if they have played the last played map.
    /// </summary>
    private async Task<IReadOnlyList<PlayerScoreResult>> GetRecentScores()
    {
        var players = context.MultiplayerLobby.Players.Where(x => x is { Id: not null, Score: > 0 }).ToList();
        var grabScoreTasks = new List<Task<APIResult<osu.NET.Models.Scores.Score[]>>>();

        for (int i = 0; i < players.Count; i++)
        {
            var index = i;

            grabScoreTasks.Add(Task.Run(async () =>
            {
                await Task.Delay(index * 250);

                Log.Information($"FunCommandsBehavior: Executing osu! API call for {players[index].Name}");

                return await context.UsingApiClient(async (apiClient) =>
                    await apiClient.GetUserScoresAsync(players[index].Id!.Value, UserScoreType.Recent, true, true,
                        Ruleset.Osu, limit: 1, offset: 0));
            }));
        }

        await Task.WhenAll(grabScoreTasks);

        return players.Select(player => new PlayerScoreResult(player,
                grabScoreTasks.Select(x => x.Result?.Value?.FirstOrDefault()).ToList()
                    .FirstOrDefault(x => x?.UserId == player.Id!)))
            .ToList();
    }
}