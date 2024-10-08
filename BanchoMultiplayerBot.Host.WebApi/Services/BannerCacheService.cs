﻿using BanchoMultiplayerBot.Host.WebApi.Data;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class BannerCacheService
{
    /// <summary>
    /// The final constructed .svg
    /// </summary>
    public string OutputCache { get; set; } = string.Empty;
        
    /// <summary>
    /// Last time the svg banner was constructed
    /// </summary>
    public DateTime CacheUpdateTime { get; set; }

    /// <summary>
    /// Base 64 encoded placeholder image, if we couldn't retrieve the beatmap image.
    /// </summary>
    public string PlaceholderImage { get; set; } = string.Empty;

    /// <summary>
    /// Cached beatmap images
    /// </summary>
    public BeatmapCoverData?[] BeatmapCoverCache { get; set; } = new BeatmapCoverData[4];
}