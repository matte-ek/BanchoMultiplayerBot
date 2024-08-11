using BanchoMultiplayerBot.Osu.Interfaces;

namespace BanchoMultiplayerBot.Osu.Data;

public class PlayPerformanceInfo : IPerformanceInfo
{
    public int PerformancePoints { get; init; }
    public int MaximumPerformancePoints { get; init; }
}