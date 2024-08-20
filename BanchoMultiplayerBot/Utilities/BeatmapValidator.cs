using System.Globalization;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Osu.Models;
using BanchoSharp.Multiplayer;
using OsuSharp.Enums;
using OsuSharp.Models.Beatmaps;
using Serilog;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Utility class to check if a map is within a lobby's regulations
/// </summary>
public class MapValidator(LobbyConfiguration lobbyConfiguration, MapManagerBehaviorConfig mapManagerBehaviorConfig)
{
    public async Task<MapStatus> ValidateBeatmap(DifficultyAttributes difficultyAttributes, BeatmapExtended? beatmapInfo)
    {
        // Allow validation with only difficulty attributes
        if (beatmapInfo != null)
        {
            if (await IsBannedBeatmap(beatmapInfo))
                return MapStatus.Banned;
            if (!IsAllowedBeatmapGameMode(beatmapInfo))
                return MapStatus.GameMode;
            if (!IsAllowedBeatmapLength(beatmapInfo))
                return MapStatus.Length;   
            //if (!IsDownloadable(beatmapInfo))
            //    return MapStatus.Removed;
        }
        
        if (!IsAllowedBeatmapStarRating(difficultyAttributes))
            return MapStatus.StarRating;
        
        return MapStatus.Ok;
    }

    private bool IsAllowedBeatmapStarRating(DifficultyAttributes beatmap)
    {
        if (!mapManagerBehaviorConfig.LimitStarRating)
            return true;

        var minRating = mapManagerBehaviorConfig.MinimumStarRating;
        var maxRating = mapManagerBehaviorConfig.MaximumStarRating;

        if (mapManagerBehaviorConfig.StarRatingErrorMargin != null)
        {
            minRating -= mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
            maxRating += mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
        }

        return maxRating >= beatmap.StarRating && beatmap.StarRating >= minRating;
    }

    private bool IsAllowedBeatmapLength(BeatmapExtended beatmap)
    {
        if (!mapManagerBehaviorConfig.LimitMapLength)
            return true;

        return mapManagerBehaviorConfig.MaximumMapLength >= beatmap.TotalLength.TotalSeconds &&
               beatmap.TotalLength.TotalSeconds >= mapManagerBehaviorConfig.MinimumMapLength;
    }

    private bool IsAllowedBeatmapGameMode(BeatmapExtended beatmap)
    {
        if (lobbyConfiguration.Mode == null)
            return true;
        
        return lobbyConfiguration.Mode switch
        {
            GameMode.osu => beatmap.Ruleset == Ruleset.Osu,
            GameMode.osuTaiko => beatmap.Ruleset == Ruleset.Taiko,
            GameMode.osuCatch => beatmap.Ruleset == Ruleset.Catch,
            GameMode.osuMania => beatmap.Ruleset == Ruleset.Mania,
            _ => false
        };
    }

    private static async Task<bool> IsBannedBeatmap(BeatmapExtended beatmap)
    {
        using var mapBanRepository = new MapBanRepository();

        return await mapBanRepository.IsMapBanned(beatmap.SetId, beatmap.Id);
    }

    private static bool IsDownloadable(BeatmapExtended beatmap)
    {
        return (beatmap as Beatmap).Set?.Availability?.IsDownloadDisabled == true;
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