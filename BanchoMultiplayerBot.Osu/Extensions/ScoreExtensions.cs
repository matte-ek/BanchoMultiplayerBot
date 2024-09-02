using OsuSharp.Models.Scores;
using Serilog;

namespace BanchoMultiplayerBot.Osu.Extensions;

public static class ScoreExtensions
{
    private static readonly Dictionary<string, ApiMods> ModsAbbreviationMap = new()
    {
        { "NF", ApiMods.NoFail },
        { "EZ", ApiMods.Easy },
        { "HT", ApiMods.HalfTime },
        { "HD", ApiMods.Hidden },
        { "FI", ApiMods.FadeIn },
        { "HR", ApiMods.HardRock },
        { "FL", ApiMods.Flashlight },
        { "DT", ApiMods.DoubleTime },
        { "NC", ApiMods.Nightcore },
        { "SD", ApiMods.SuddenDeath },
        { "SO", ApiMods.SpunOut },
        { "RX", ApiMods.Relax },
        { "AP", ApiMods.Relax2 }
    };

    public static int GetModsBitset(string[] input)
    {
        int bitset = 0;
        
        foreach (var mod in input)
        {
            if (ModsAbbreviationMap.TryGetValue(mod.ToUpper(), out var modEnum))
            {
                bitset |= (int)modEnum;
            }
            else
            {
                Log.Error($"GetModsBitset: Unknown mod abbreviation: {mod}");
            }
        }

        return bitset;
    }

    public static int GetModsBitset(this Score score) => GetModsBitset(score.Mods);
}