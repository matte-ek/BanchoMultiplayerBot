using System.Globalization;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Utility class to check if a map is within a lobby's regulations
/// </summary>
public class MapValidator
{
    private readonly Lobby _lobby;

    public MapValidator(Lobby lobby)
    {
        _lobby = lobby;
    }

    public async Task<MapStatus> ValidateBeatmap(BeatmapModel beatmap)
    {
        var hostIsAdministrator = _lobby.MultiplayerLobby.Host is not null &&
                                  await Bot.IsAdministrator(_lobby.MultiplayerLobby.Host.Name);

        if (hostIsAdministrator)
            return MapStatus.Ok;
        
        if (await IsBannedBeatmap(beatmap))
            return MapStatus.Banned;
        if (!IsAllowedBeatmapGameMode(beatmap))
            return MapStatus.GameMode;
        if (!IsAllowedBeatmapLength(beatmap))
            return MapStatus.Length;
        if (!IsAllowedBeatmapStarRating(beatmap))
            return MapStatus.StarRating;

        return MapStatus.Ok;
    }

    private bool IsAllowedBeatmapStarRating(BeatmapModel beatmap)
    {
        if (!_lobby.Configuration.LimitStarRating)
            return true;
        if (beatmap.DifficultyRating == null)
            return false;

        var config = _lobby.Configuration;
        var minRating = config.MinimumStarRating;
        var maxRating = config.MaximumStarRating;

        if (config.StarRatingErrorMargin != null)
        {
            minRating -= config.StarRatingErrorMargin.Value;
            maxRating += config.StarRatingErrorMargin.Value;
        }

        var mapStarRating = float.Parse(beatmap.DifficultyRating, CultureInfo.InvariantCulture);

        return maxRating >= mapStarRating && mapStarRating >= minRating;
    }

    private bool IsAllowedBeatmapLength(BeatmapModel beatmap)
    {
        if (!_lobby.Configuration.LimitMapLength)
            return true;
        if (beatmap.TotalLength == null)
            return false;

        var mapLength = int.Parse(beatmap.TotalLength, CultureInfo.InvariantCulture);

        return _lobby.Configuration.MaximumMapLength >= mapLength && mapLength >= _lobby.Configuration.MinimumMapLength;
    }

    private bool IsAllowedBeatmapGameMode(BeatmapModel beatmap)
    {
        if (_lobby.Configuration.Mode == null)
            return true;

        // Game modes are defined in the API as:
        // 0 - osu!standard
        // 1 - osu!taiko
        // 2 - osu!catch
        // 3 - osu!mania

        var beatmapMode = beatmap.Mode;

        if (beatmapMode != null)
        {
            return _lobby.Configuration.Mode switch
            {
                GameMode.osu => beatmapMode == "0",
                GameMode.osuTaiko => beatmapMode == "1",
                GameMode.osuCatch => beatmapMode == "2",
                GameMode.osuMania => beatmapMode == "3",
                _ => false
            };
        }

        Log.Error($"No beatmap mode for map {beatmap.BeatmapId}");

        return false;
    }

    private static async Task<bool> IsBannedBeatmap(BeatmapModel beatmap)
    {
        if (beatmap.BeatmapsetId == null ||
            beatmap.BeatmapId == null)
            return false;

        try
        {
            using var mapBanRepository = new MapBanRepository();

            return await mapBanRepository.IsMapBanned(int.Parse(beatmap.BeatmapsetId), int.Parse(beatmap.BeatmapId));
        }
        catch (Exception e)
        {
            Log.Error($"Error while querying map ban status: {e}");
            return false;
        }
    }

    public enum MapStatus
    {
        Ok,
        StarRating,
        Length,
        GameMode,
        Banned
    }
}