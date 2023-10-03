using System.Text;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class FunCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private bool _flushedPlaytime;

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
        _lobby.OnUserMessage += OnUserMessage;
        _lobby.OnAdminMessage += OnAdminMessage;

        var mapManagerBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
        if (mapManagerBehaviour != null)
        {
            _mapManagerBehaviour = (MapManagerBehaviour)mapManagerBehaviour;
        }
    }

    public async Task FlushPlaytime()
    {
        try
        {
            using var userRepository = new UserRepository();

            foreach (var player in _lobby.MultiplayerLobby.Players)
            {
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                user.Playtime += (int)(DateTime.Now - player.JoinTime).TotalSeconds;
            }

            await userRepository.Save();

            _flushedPlaytime = true;
        }
        catch (Exception e)
        {
            Log.Error($"Exception at FunCommands.FlushPlaytime(): {e}");
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

            if (msg.Content.ToLower().Equals("!playtime") || msg.Content.ToLower().Equals("!pt"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                var currentPlaytime = DateTime.Now - player.JoinTime;
                var totalPlaytime = TimeSpan.FromSeconds(user.Playtime) + currentPlaytime; // We add current play-time since it's only appended after the player disconnects.

                msg.Reply(
                    _lobby.Bot.RuntimeInfo.StartTime.AddMinutes(2) >= player.JoinTime
                        ? $"{msg.Sender} has been here since last bot restart, {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} in total)"
                        : $"{msg.Sender} has been here for {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} ({totalPlaytime.TotalHours:F0}h) in total)");
            }

            if (msg.Content.ToLower().Equals("!playstats") || msg.Content.ToLower().Equals("!ps"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                msg.Reply($"{msg.Sender} has played {user.MatchesPlayed} matches with a total of {user.NumberOneResults} #1's");
            }

            if ((msg.Content.ToLower().Equals("!mapstats") || msg.Content.ToLower().Equals("!ms")) && _mapManagerBehaviour.CurrentBeatmap != null)
            {
                using var gameRepository = new GameRepository();

                var beatmapId = _mapManagerBehaviour.CurrentBeatmap.Id;
                var totalPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, null);
                var pastWeekPlayCount = await gameRepository.GetGameCountByMapId(beatmapId, DateTime.Now.AddDays(-7));
                var recentGames = await gameRepository.GetRecentGames(beatmapId, 10);

                if (recentGames.Count >= 3)
                {
                    List<float> leaveRatio = new();
                    List<float> passRatio = new();

                    foreach (var game in recentGames)
                    {
                        if (game.PlayerPassedCount == -1)
                        {
                            continue;
                        }
                        
                        leaveRatio.Add((float)game.PlayerFinishCount / game.PlayerCount);
                        passRatio.Add((float)game.PlayerPassedCount / game.PlayerFinishCount);
                    }

                    if (passRatio.Count < 3)
                    {
                        msg.Reply(totalPlayCount != 0
                            ? $"This map has been played a total of {totalPlayCount} times ({pastWeekPlayCount} times past week)!"
                            : $"This map has been played a total of {totalPlayCount} times!");
                    }
                    else
                    {
                        var avgLeavePercentage = 100f - MathF.Min(leaveRatio.Average() * 100f, 100f);
                        var avgPassPercentage = MathF.Min(passRatio.Average() * 100f, 100f);

                        msg.Reply($"This map has been played a total of {totalPlayCount} times! ({pastWeekPlayCount} times past week), {avgLeavePercentage:0}% of the players usually leave the lobby, and {avgPassPercentage:0}% will pass the map!");   
                    }
                }
                else
                {
                    msg.Reply(totalPlayCount != 0
                        ? $"This map has been played a total of {totalPlayCount} times ({pastWeekPlayCount} times past week)!"
                        : $"This map has been played a total of {totalPlayCount} times!");
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async void OnAdminMessage(IPrivateIrcMessage msg)
    {
        try
        {
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
            if (_flushedPlaytime)
                return;

            using var userRepository = new UserRepository();
            var user = await userRepository.FindUser(args.Player.Name) ?? await userRepository.CreateUser(args.Player.Name);

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
        var playerPassedCount = recentScores.Count(x => x.Score?.Score != "F");

        var game = new Game()
        {
            BeatmapId = _mapManagerBehaviour.CurrentBeatmap.Id,
            Time = DateTime.Now,
            PlayerCount = _startPlayerCount,
            PlayerFinishCount = playerFinishCount,
            PlayerPassedCount = playerPassedCount
        };

        await gameRepository.AddGame(game);
    }

    private async Task StorePlayerFinishData(IReadOnlyList<PlayerScoreResult> recentScores)
    {
        using var userRepository = new UserRepository();

        var highestScorePlayer = recentScores.Where(x => x.Score?.Score != "F").MaxBy(x => x.Player.Score);

        if (_lobby.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
        {
            var user = await userRepository.FindUser(highestScorePlayer.Player.Name) ?? await userRepository.CreateUser(highestScorePlayer.Player.Name);

            user.NumberOneResults++;
        }

        foreach (var result in recentScores)
        {
            var user = await userRepository.FindUser(result.Player.Name) ?? await userRepository.CreateUser(result.Player.Name);

            if (result.Score?.Score != "F")
                user.MatchesPlayed++;
        }

        await userRepository.Save();
    }

    private async Task<IReadOnlyList<PlayerScoreResult>> GetRecentScores()
    {
        var players = _lobby.MultiplayerLobby.Players.Where(x => x.Id != null && x.Score > 0).ToList();
        var scores = await _lobby.Bot.OsuApi.GetRecentScoresBatch(players.Select(x => x.Id.ToString()).ToList());
        
        return players.Select((t, i) => new PlayerScoreResult(t, scores[i]?.BeatmapId == _lastPlayedBeatmapId.ToString() ? scores[i] : null)).ToList();
    }
}