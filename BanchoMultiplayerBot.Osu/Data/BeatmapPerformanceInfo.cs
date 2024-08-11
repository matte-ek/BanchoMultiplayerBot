﻿using BanchoMultiplayerBot.Osu.Interfaces;

namespace BanchoMultiplayerBot.Osu.Data;

public class BeatmapPerformanceInfo : IPerformanceInfo
{
    public int Performance100 { get; init; }
    public int Performance98 { get; init; }
    public int Performance95 { get; init; }
}