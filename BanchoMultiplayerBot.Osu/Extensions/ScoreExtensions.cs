﻿using BanchoMultiplayerBot.Osu.Data;
using OsuSharp.Models.Scores;
using Serilog;

namespace BanchoMultiplayerBot.Osu.Extensions;

public static class ScoreExtensions
{
    private static readonly Dictionary<string, OsuMods> ModsAbbreviationMap = new()
    {
        { "NF", OsuMods.NoFail },
        { "EZ", OsuMods.Easy },
        { "HT", OsuMods.HalfTime },
        { "HD", OsuMods.Hidden },
        { "FI", OsuMods.FadeIn },
        { "HR", OsuMods.HardRock },
        { "FL", OsuMods.Flashlight },
        { "DT", OsuMods.DoubleTime },
        { "NC", OsuMods.Nightcore },
        { "SD", OsuMods.SuddenDeath },
        { "SO", OsuMods.SpunOut },
        { "RX", OsuMods.Relax },
        { "AP", OsuMods.Relax2 }
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