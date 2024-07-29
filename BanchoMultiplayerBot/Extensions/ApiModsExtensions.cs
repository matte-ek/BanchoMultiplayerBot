using System.Text;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Extensions;

public static class ApiModsExtensions
{
    public static string ToAbbreviatedForm(this ModsModel mods, bool showFreemod = true)
    {
        var ret = new StringBuilder(16);

        // Just going to keep appending to this string, to get everything
        // in the "right" order. This might not be the cleanest solution, 
        // but I can't think of anything better right now.

        if ((mods & ModsModel.Relax) != 0)
            ret.Append("RX");
        if ((mods & ModsModel.Relax2) != 0)
            ret.Append("AP");
        if ((mods & ModsModel.SpunOut) != 0)
            ret.Append("SO");
        if ((mods & ModsModel.Easy) != 0)
            ret.Append("EZ");
        if ((mods & ModsModel.NoFail) != 0)
            ret.Append("NF");
        if ((mods & ModsModel.Hidden) != 0)
            ret.Append("HD");
        if ((mods & ModsModel.HalfTime) != 0)
            ret.Append("HT");
        if ((mods & ModsModel.DoubleTime) != 0)
            ret.Append("DT");
        if ((mods & ModsModel.Nightcore) != 0)
            ret.Append("NC");
        if ((mods & ModsModel.HardRock) != 0)
            ret.Append("HR");
        if ((mods & ModsModel.SuddenDeath) != 0)
            ret.Append("SD");
        if ((mods & ModsModel.Perfect) != 0)
            ret.Append("PF");
        if ((mods & ModsModel.Flashlight) != 0)
            ret.Append("FL");

        if (ret.Length == 0)
            ret.Append("None");

        return ret.ToString();
    }

    public static ModsModel FromAbbreviatedForm(string input)
    {
        ModsModel ret = ModsModel.None;

        if (input.Length <= 1)
        {
            return ret;
        }

        for (int i = 0; i < input.Length; i += 2)
        {
            var mod = input.Substring(i, 2);

            if (!_modsAbbreviationMap.ContainsKey(mod))
            {
                // Probably some mod we don't care about
                continue;
            }

            ret |= _modsAbbreviationMap[mod];
        }

        return ret;
    }

    private static readonly Dictionary<string, ModsModel> _modsAbbreviationMap = new()
    {
        { "NF", ModsModel.NoFail },
        { "EZ", ModsModel.Easy },
        { "HT", ModsModel.HalfTime },
        { "HD", ModsModel.Hidden },
        { "HR", ModsModel.HardRock },
        { "FL", ModsModel.Flashlight },
        { "DT", ModsModel.DoubleTime },
        { "SD", ModsModel.SuddenDeath },
        { "SO", ModsModel.SpunOut },
        { "RX", ModsModel.Relax },
        { "AP", ModsModel.Relax2 }
    };
}