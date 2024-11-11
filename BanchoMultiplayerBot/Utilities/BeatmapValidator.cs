using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using OsuSharp.Enums;
using OsuSharp.Models.Beatmaps;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Utility class to check if a map is within a lobby's regulations
/// </summary>
public class MapValidator(ILobby lobby, LobbyConfiguration lobbyConfiguration, MapManagerBehaviorConfig mapManagerBehaviorConfig)
{
    public async Task<MapStatus> ValidateBeatmap(DifficultyAttributes difficultyAttributes, BeatmapExtended? beatmapInfo, bool useBeatmapStarRating = false)
    {
        if (await IsHostAdministrator())
            return MapStatus.Ok;
        if (!IsAllowedBeatmapStarRating(difficultyAttributes, beatmapInfo, useBeatmapStarRating))
            return MapStatus.StarRating;
        if (await IsBannedBeatmap(beatmapInfo))
            return MapStatus.Banned;
        if (!IsAllowedBeatmapGameMode(beatmapInfo))
            return MapStatus.GameMode;
        if (!IsAllowedBeatmapLength(beatmapInfo))
            return MapStatus.Length;   

        return MapStatus.Ok;
    }

    private bool IsAllowedBeatmapStarRating(DifficultyAttributes beatmap, BeatmapExtended? beatmapInfo, bool useBeatmapStarRating)
    {
        if (!mapManagerBehaviorConfig.LimitStarRating)
            return true;

        var starRating = useBeatmapStarRating ? beatmapInfo?.DifficultyRating : beatmap.DifficultyRating;

        starRating ??= beatmap.DifficultyRating;
        
        var minRating = mapManagerBehaviorConfig.MinimumStarRating;
        var maxRating = mapManagerBehaviorConfig.MaximumStarRating;

        if (mapManagerBehaviorConfig.StarRatingErrorMargin != null)
        {
            minRating -= mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
            maxRating += mapManagerBehaviorConfig.StarRatingErrorMargin.Value;
        }

        return maxRating >= starRating && starRating >= minRating;
    }

    private bool IsAllowedBeatmapLength(BeatmapExtended? beatmap)
    {
        if (beatmap == null)
        {
            // We don't know any better, fallback to "it's okay".
            return true;
        }
        
        if (!mapManagerBehaviorConfig.LimitMapLength)
        {
            return true;
        }

        return mapManagerBehaviorConfig.MaximumMapLength >= beatmap.TotalLength.TotalSeconds &&
               beatmap.TotalLength.TotalSeconds >= mapManagerBehaviorConfig.MinimumMapLength;
    }

    private bool IsAllowedBeatmapGameMode(BeatmapExtended? beatmap)
    {
        if (beatmap == null)
        {
            // We don't know any better, fallback to "it's okay".
            return true;
        }

        if (lobbyConfiguration.Mode == null)
        {
            return true;
        }
        
        return lobbyConfiguration.Mode switch
        {
            GameMode.osu => beatmap.Ruleset == Ruleset.Osu,
            GameMode.osuTaiko => beatmap.Ruleset == Ruleset.Taiko,
            GameMode.osuCatch => beatmap.Ruleset == Ruleset.Catch,
            GameMode.osuMania => beatmap.Ruleset == Ruleset.Mania,
            _ => false
        };
    }

    private static async Task<bool> IsBannedBeatmap(BeatmapExtended? beatmap)
    {
        if (beatmap == null)
        {
            // We don't know any better, fallback to "it's okay".
            return false;
        }
        
        await using var mapBanRepository = new MapBanRepository();

        return await mapBanRepository.IsMapBanned(beatmap.SetId, beatmap.Id);
    }

    private static bool IsDownloadable(BeatmapExtended beatmap)
    {
        return (beatmap as Beatmap).Set?.Availability?.IsDownloadDisabled == true;
    }

    private async Task<bool> IsHostAdministrator()
    {
        if (lobby.MultiplayerLobby?.Host == null)
        {
            return false;
        }

        await using var userRepository = new UserRepository();
            
        var user = await userRepository.FindOrCreateUserAsync(lobby.MultiplayerLobby!.Host.Name);

        return user.Administrator;
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