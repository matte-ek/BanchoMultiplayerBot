using System.Globalization;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Osu.Models;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Utilities;


/// <summary>
/// Utility class to check if a map is within a lobby's regulations
/// </summary>
public class MapValidator(LobbyConfiguration lobbyConfiguration, MapManagerBehaviorConfig mapManagerBehaviorConfig)
{
    public async Task<MapStatus> ValidateBeatmap(BeatmapModel beatmap)
    {
        if (await IsBannedBeatmap(beatmap))
            return MapStatus.Banned;
        if (!IsAllowedBeatmapGameMode(beatmap))
            return MapStatus.GameMode;
        if (!IsAllowedBeatmapLength(beatmap))
            return MapStatus.Length;
        if (!IsAllowedBeatmapStarRating(beatmap))
            return MapStatus.StarRating;
        if (!IsDownloadable(beatmap))
            return MapStatus.Removed;

        return MapStatus.Ok;
    }
    
    private bool IsAllowedBeatmapStarRating(BeatmapModel beatmap)
    {
        if (!mapManagerBehaviorConfig.LimitStarRating)
            return true;
        if (beatmap.DifficultyRating == null)
            return false;

        var minRating = mapManagerBehaviorConfig.MinimumStarRating;
        var maxRating = mapManagerBehaviorConfig.MaximumStarRating;

        if (mapManagerBehaviorConfig.StarRatingErrorMargin != null)
        {
            minRating -= mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
            maxRating += mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
        }

        var mapStarRating = float.Parse(beatmap.DifficultyRating, CultureInfo.InvariantCulture);

        return maxRating >= mapStarRating && mapStarRating >= minRating;
    }

    private bool IsAllowedBeatmapLength(BeatmapModel beatmap)
    {
        if (!mapManagerBehaviorConfig.LimitMapLength)
            return true;
        if (beatmap.TotalLength == null)
            return false;

        var mapLength = int.Parse(beatmap.TotalLength, CultureInfo.InvariantCulture);

        return mapManagerBehaviorConfig.MaximumMapLength >= mapLength && mapLength >= mapManagerBehaviorConfig.MinimumMapLength;
    }

    private bool IsAllowedBeatmapGameMode(BeatmapModel beatmap)
    {
        if (lobbyConfiguration.Mode == null)
            return true;

        // Game modes are defined in the API as:
        // 0 - osu!standard
        // 1 - osu!taiko
        // 2 - osu!catch
        // 3 - osu!mania

        var beatmapMode = beatmap.Mode;

        if (beatmapMode != null)
        {
            return lobbyConfiguration.Mode switch
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

        using var mapBanRepository = new MapBanRepository();

        return await mapBanRepository.IsMapBanned(int.Parse(beatmap.BeatmapsetId), int.Parse(beatmap.BeatmapId));
    }

    private static bool IsDownloadable(BeatmapModel beatmap)
    {
        if (beatmap.DownloadUnavailable == null)
            return true;
        if (beatmap.AudioUnavailable == null)
            return true;
        
        return !(beatmap.DownloadUnavailable == "1" || beatmap.AudioUnavailable == "1");
    }
    
    public enum MapStatus
    {
        Ok,
        StarRating,
        Length,
        GameMode,
        Banned,
        Removed
    }
}