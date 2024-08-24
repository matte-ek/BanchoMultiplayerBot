using BanchoSharp.Multiplayer;
using OsuSharp.Models.Scores;

namespace BanchoMultiplayerBot.Osu.Extensions;

public static class ScoreExtensions
{
    private static readonly Dictionary<string, Mods> ModsAbbreviationMap = new()
    {
        { "NF", Mods.NoFail },
        { "EZ", Mods.Easy },
        { "HT", Mods.HalfTime },
        { "HD", Mods.Hidden },
        { "FI", Mods.FadeIn },
        { "HR", Mods.HardRock },
        { "FL", Mods.Flashlight },
        { "DT", Mods.DoubleTime },
        { "NC", Mods.Nightcore },
        { "SD", Mods.SuddenDeath },
        { "SO", Mods.SpunOut },
        { "RX", Mods.Relax },
        { "AP", Mods.Autopilot },
        { "FM", Mods.Freemod }
    };
    
    public static int GetModsBitset(this Score score)
    {
        int bitset = 0;
        
        foreach (var mod in score.Mods)
        {
            if (ModsAbbreviationMap.TryGetValue(mod, out var modEnum))
            {
                bitset |= (int)modEnum;
            }
        }

        return bitset;
    }
}