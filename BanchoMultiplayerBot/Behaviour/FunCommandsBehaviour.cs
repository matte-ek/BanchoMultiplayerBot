using System.Text;
using System.Web;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class FunCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private bool _hasGameData;
    private int _startPlayerCount;

    private int _lastPlayedBeatmapId = 0;

    private MapManagerBehaviour? _mapManagerBehaviour;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        _lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
        _lobby.OnUserMessage += OnUserMessage;
        _lobby.OnAdminMessage += OnAdminMessage;

        var mapManagerBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
        if (mapManagerBehaviour != null)
        {
            _mapManagerBehaviour = (MapManagerBehaviour)mapManagerBehaviour;
        }
    }

    private void OnSettingsUpdated()
    {
        if (!_lobby.IsRecovering)
        {
            return;
        }

        if (_lobby.Configuration.PlayerPlaytime == null ||
            _lobby.Configuration.PlayerPlaytime.Length == 0)
        {
            Log.Warning("Unable to restore player playtime, no data.");
            return;
        }

        foreach (var player in _lobby.Configuration.PlayerPlaytime)
        {
            var multiplayerPlayer = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == player.Name);

            if (multiplayerPlayer is null)
            {
                Log.Warning($"Failed to restore playtime for player {player.Name}, player doesn't exist.");
                continue;
            }

            multiplayerPlayer.JoinTime = DateTime.FromBinary(player.JoinTime);
        }
    }

    private async void OnUserMessage(PlayerMessage msg)
    {
        try
        {
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == msg.Sender);

            if (player is null)
            {
                return;
            }

            if (_mapManagerBehaviour == null)
            {
                return;
            }

            var name = player.Id.ToString() ?? HttpUtility.UrlEncode(player.Name);
            var nameIdentifier = _lobby.Bot.Configuration.StatisticsUrl != null
                ? $"[{_lobby.Bot.Configuration.StatisticsUrl}/user/{name} {player.Name}]"
                : player.Name;

            if (msg.Content.ToLower().Equals("!playtime") || msg.Content.ToLower().Equals("!pt"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                var currentPlaytime = DateTime.Now - player.JoinTime;
                var totalPlaytime =
                    TimeSpan.FromSeconds(user.Playtime) +
                    currentPlaytime; // We add current play-time since it's only appended after the player disconnects.

                msg.Reply(
                    $"{nameIdentifier} has been here for {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} ({totalPlaytime.TotalHours:F0}h) in total).");
            }

            if (msg.Content.ToLower().Equals("!playstats") || msg.Content.ToLower().Equals("!ps"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                msg.Reply(
                    $"{nameIdentifier} has played {user.MatchesPlayed} matches with a total of {user.NumberOneResults} #1's.");
            }

            if (_mapManagerBehaviour.CurrentBeatmap != null && player.Id != null)
            {
                if (msg.Content.ToLower().Equals("!personalmapstats") || msg.Content.ToLower().Equals("!pms"))
                {
                    using var scoreRepository = new ScoreRepository();
                    var scores = await scoreRepository.GetScoresByMapAndPlayerId(player.Id.Value, _mapManagerBehaviour.CurrentBeatmap.Id);
                
                    var failCount = scores.Count(x => x.Rank == 1);
                    var passCount = scores.Count - failCount;
                    var passFail =  failCount >= passCount ? "fail" : "pass";
                    var mostCommonRank = scores.Select(x => x.Rank)
                        .GroupBy(i=>i)
                        .OrderByDescending(grp=>grp.Count())
                        .Select(grp=>grp.Key).FirstOrDefault();
                    var avgAccuracy = scores.Count > 0 ? scores.Average(x => x.CalculateAccuracy()) : 0;

                    if (scores.Count > 0)
                    {
                        _lobby.SendMessage(passFail == "fail"
                            ? $"{nameIdentifier}, you've played this map {scores.Count} times in this lobby! You usually {passFail} the map! :("
                            : $"{nameIdentifier}, you've played this map {scores.Count} times in this lobby! You usually {passFail} the map with an {ScoreModel.GetRankString(mostCommonRank!)} rank, average accuracy: {avgAccuracy:0.00}%!");
                    }
                    else
                    {
                        _lobby.SendMessage($"{nameIdentifier}, you haven't played this map in this lobby yet!");  
                    }
                }
                
                if (msg.Content.ToLower().Equals("!bestmapstats") || msg.Content.ToLower().Equals("!bms"))
                {
                    using var scoreRepository = new ScoreRepository();
                    
                    var scores = await scoreRepository.GetScoresByMapAndPlayerId(player.Id.Value, _mapManagerBehaviour.CurrentBeatmap.Id);
                    var bestScore = scores.MaxBy(x => x.TotalScore);
                    var bestScoreAcc = bestScore?.CalculateAccuracy();
                    
                    _lobby.SendMessage(bestScore != null
                        ? $"{nameIdentifier}, your best score on this map in this lobby is an {ScoreModel.GetRankString(bestScore.Rank)} rank with {bestScoreAcc:0.00}% accuracy and x{bestScore.MaxCombo} combo, {bestScore.Count300}/{bestScore.Count100}/{bestScore.Count50}/{bestScore.CountMiss}!"
                        : $"{nameIdentifier}, you haven't played this map in this lobby yet!");
                }
                
                if (msg.Content.ToLower().Equals("!mapstats") || msg.Content.ToLower().Equals("!ms"))
                {
                    using var gameRepository = new GameRepository();
                    using var scoreRepository = new ScoreRepository();
                    
                    var beatmapId = _mapManagerBehaviour.CurrentBeatmap.Id;
                    var totalPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, null);
                    var pastWeekPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, DateTime.Now.AddDays(-7));
                    
                    var recentGames = await gameRepository.GetRecentGames(beatmapId, 50);
                    var recentScores = await scoreRepository.GetScoresByMapId(beatmapId, 50);
                    
                    var mapPosition = await scoreRepository.GetMapPlayCountByLobbyId(_lobby.LobbyIndex, beatmapId);
                    
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
                        var avgAccuracy = recentScores.Average(x => x.CalculateAccuracy());
      
                        outputMessage.Append($" | Average accuracy: {avgAccuracy:0.00}%");
                    }
                    
                    if (recentGames.Any())
                    {
                        List<float> leaveRatio = new();
                        List<float> passRatio = new();

                        // Calculate percentages for the last 10 games
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

                        if (passRatio.Any())
                        {
                            var avgLeavePercentage = 100f - MathF.Min(leaveRatio.Average() * 100f, 100f);
                            var avgPassPercentage = MathF.Min(passRatio.Average() * 100f, 100f);

                            outputMessage.Append($" | {avgLeavePercentage:0}% leave the lobby and {avgPassPercentage:0}% pass!");
                        }
                    }
                    
                    msg.Reply(outputMessage.ToString());
                }

                if (msg.Content.ToLower().StartsWith("!pp") &&
                    _mapManagerBehaviour.CurrentBeatmap != null &&
                    _lobby.Bot.PerformancePointCalculator != null)
                {
                    ModsModel mods = ModsModel.None;
                    
                    if (msg.Content.Length >= 6)
                    {
                        mods = ApiModsExtensions.FromAbbreviatedForm(msg.Content[4..].ToUpper());
                    }

                    var ppInfo = await _lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(_mapManagerBehaviour.CurrentBeatmap.Id, (int)mods);

                    msg.Reply(ppInfo != null
                        ? $"{nameIdentifier}, 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp"
                        : "Error calculating performance points");
                }
            }
        }
        catch (Exception e)
        {
            Log.Warning($"Exception at FunCommands.OnUserMessage(): {e}");
        }
    }

    private async void OnAdminMessage(IPrivateIrcMessage msg)
    {
        try
        {
            // !mvname <source> <target>
            // Moves the stats from source to target
            if (msg.Content.StartsWith("!mvname "))
            {
                using var userRepository = new UserRepository();
                var args = msg.Content.Split(' ');

                var sourceUser = await userRepository.FindUser(args[1]);
                var targetUser = await userRepository.FindUser(args[2]);

                if (sourceUser == null ||
                    targetUser == null)
                {
                    _lobby.SendMessage("Failed to find source/target player.");
                    return;
                }

                targetUser.MatchesPlayed += sourceUser.MatchesPlayed;
                targetUser.NumberOneResults += sourceUser.NumberOneResults;
                targetUser.Playtime += sourceUser.Playtime;

                sourceUser.MatchesPlayed = 0;
                sourceUser.NumberOneResults = 0;
                sourceUser.Playtime = 0;

                await userRepository.Save();

                _lobby.SendMessage("Successfully moved player stats.");
            }

            if (msg.Content.StartsWith("!setstats "))
            {
                using var userRepository = new UserRepository();
                var args = msg.Content.Split(' ');

                var user = await userRepository.FindUser(args[1]);
                var playtime = int.Parse(args[2]);
                var matchesPlayed = int.Parse(args[3]);
                var numberOneResults = int.Parse(args[4]);

                if (user == null)
                {
                    return;
                }

                user.Playtime = playtime;
                user.MatchesPlayed = matchesPlayed;
                user.NumberOneResults = numberOneResults;

                await userRepository.Save();
            }

            if (msg.Content.StartsWith("!getstats "))
            {
                using var userRepository = new UserRepository();
                var args = msg.Content.Split(' ');
                var user = await userRepository.FindUser(args[1]);

                if (user == null)
                {
                    return;
                }

                _lobby.SendMessage($"{user.Playtime} | {user.MatchesPlayed} | {user.NumberOneResults}");
            }
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void OnMatchStarted()
    {
        try
        {
            _hasGameData = true;
            _startPlayerCount = _lobby.MultiplayerLobby.Players.Count;
        }
        catch (Exception e)
        {
            Log.Error($"Exception at FunCommands.OnMatchStarted(): {e}");
        }
    }

    private async void OnMatchFinished()
    {
        try
        {
            _lastPlayedBeatmapId = _mapManagerBehaviour!.CurrentBeatmap!.Id;

            // Give osu! some time to work out the scores.
            await Task.Delay(TimeSpan.FromSeconds(5));

            var recentScores = await GetRecentScores();

            await StoreGameData(recentScores);
            await StorePlayerFinishData(recentScores);
            await AnnounceLeaderboardResults(recentScores);
        }
        catch (Exception e)
        {
            Log.Error($"Exception at FunCommands.OnMatchFinished(): {e}");
        }
    }

    private async void OnPlayerDisconnected(PlayerDisconnectedEventArgs args)
    {
        try
        {
            using var userRepository = new UserRepository();
            var user = await userRepository.FindUser(args.Player.Name) ??
                       await userRepository.CreateUser(args.Player.Name);

            user.Playtime += (int)(DateTime.Now - args.Player.JoinTime).TotalSeconds;

            await userRepository.Save();
        }
        catch (Exception e)
        {
            Log.Error($"Exception at FunCommands.OnPlayerDisconnected(): {e}");
        }
    }

    private async Task StoreGameData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (_mapManagerBehaviour?.CurrentBeatmap == null ||
            _hasGameData == false)
        {
            return;
        }

        using var gameRepository = new GameRepository();

        var playerFinishCount = recentScores.Count;
        var playerPassedCount = recentScores.Count(x => x.Score?.Rank != "F");

        var game = new Game()
        {
            BeatmapId = _mapManagerBehaviour.CurrentBeatmap.Id,
            Time = DateTime.Now,
            PlayerCount = _startPlayerCount,
            PlayerFinishCount = playerFinishCount,
            PlayerPassedCount = playerPassedCount
        };

        await gameRepository.AddGame(game);

        await StoreScoreData(recentScores, game);
    }

    private async Task StorePlayerFinishData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        using var userRepository = new UserRepository();

        var highestScorePlayer = recentScores.Where(x => x.Score?.Rank != "F").MaxBy(x => x.Player.Score);

        if (_lobby.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
        {
            var user = await userRepository.FindUser(highestScorePlayer.Player.Name) ??
                       await userRepository.CreateUser(highestScorePlayer.Player.Name);

            user.NumberOneResults++;
        }

        foreach (var result in recentScores)
        {
            var user = await userRepository.FindUser(result.Player.Name) ??
                       await userRepository.CreateUser(result.Player.Name);

            if (result.Score?.Rank != "F")
                user.MatchesPlayed++;
        }

        await userRepository.Save();
    }

    private async Task StoreScoreData(IReadOnlyList<PlayerScoreResult> recentScores, Game game)
    {
        using var userRepository = new UserRepository();
        using var scoreRepository = new ScoreRepository();

        try
        {
            foreach (var result in recentScores)
            {
                if (result.Score == null)
                {
                    continue;
                }

                var score = result.Score;
                var user = await userRepository.FindUser(result.Player.Name) ??
                           await userRepository.CreateUser(result.Player.Name);

                var newScore = new Score()
                {
                    UserId = user.Id,
                    PlayerId = result.Player.Id,
                    LobbyId = _lobby.LobbyIndex,
                    GameId = game.Id,
                    OsuScoreId = score.ScoreId == null ? null : long.Parse(score.ScoreId),
                    BeatmapId = long.Parse(score.BeatmapId!),
                    TotalScore = long.Parse(score.Score!),
                    Rank = score.GetRankId(),
                    MaxCombo = int.Parse(score.Maxcombo!),
                    Count300 = int.Parse(score.Count300!),
                    Count100 = int.Parse(score.Count100!),
                    Count50 = int.Parse(score.Count50!),
                    CountMiss = int.Parse(score.Countmiss!),
                    Mods = int.Parse(score.EnabledMods!),
                };

                await scoreRepository.Add(newScore);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Exception at StoreScoreData(): {e}");
        }

        await scoreRepository.Save();
    }

    private async Task AnnounceLeaderboardResults(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        if (_mapManagerBehaviour?.CurrentBeatmap == null ||
            _hasGameData == false)
        {
            return;
        }

        var leaderboardScores = await _lobby.Bot.OsuApi.GetLeaderboardScores(_mapManagerBehaviour!.CurrentBeatmap!.Id);
        if (leaderboardScores == null || !leaderboardScores.Any())
        {
            return;
        }

        foreach (var score in recentScores)
        {
            var leaderboardScore = leaderboardScores.FirstOrDefault(x => x?.ScoreId == score?.Score?.ScoreId);
            if (leaderboardScore == null)
            {
                continue;
            }

            var leaderboardPosition = leaderboardScores.ToList().FindIndex(x => x?.ScoreId == score?.Score?.ScoreId);
            if (leaderboardPosition == -1)
            {
                continue;
            }

            Log.Information($"Found leaderboard score for player: {score.Player.Name}!");

            if (_lobby.Configuration.AnnounceLeaderboardResults == true)
            {
                _lobby.SendMessage(
                    $"Congratulations {score.Player.Name} for getting #{leaderboardPosition + 1} on the map's leaderboard!");
            }
        }
    }

    private async Task<IReadOnlyList<PlayerScoreResult>> GetRecentScores()
    {
        var players = _lobby.MultiplayerLobby.Players.Where(x => x.Id != null && x.Score > 0).ToList();
        var scores = await _lobby.Bot.OsuApi.GetRecentScoresBatch(players.Select(x => x.Id.ToString()).ToList());

        return players.Select((t, i) =>
                new PlayerScoreResult(t, scores[i]?.BeatmapId == _lastPlayedBeatmapId.ToString() ? scores[i] : null))
            .ToList();
    }
}