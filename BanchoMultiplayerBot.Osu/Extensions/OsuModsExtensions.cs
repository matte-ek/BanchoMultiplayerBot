using System.Text;
using BanchoMultiplayerBot.Osu.Data;

namespace BanchoMultiplayerBot.Osu.Extensions;

public static class OsuModsExtensions
{
    /// <summary>
    /// Returns a string with a shorter format, example: "HDDT"
    /// </summary>
    public static string ToAbbreviatedForm(this OsuMods mods)
    {
        var ret = new StringBuilder(16);
        
        // Just going to keep appending to this string, to get everything
        // in the "right" order. This might not be the cleanest solution, 
        // but I can't think of anything better right now.
        
        if ((mods & OsuMods.Relax) != 0)
            ret.Append("RX");
        if ((mods & OsuMods.Relax2) != 0)
            ret.Append("AP");
        if ((mods & OsuMods.SpunOut) != 0)
            ret.Append("SO");
        if ((mods & OsuMods.Easy) != 0)
            ret.Append("EZ");
        if ((mods & OsuMods.NoFail) != 0)
            ret.Append("NF");
        if ((mods & OsuMods.Hidden) != 0)
            ret.Append("HD");
        if ((mods & OsuMods.HalfTime) != 0)
            ret.Append("HT");
        if ((mods & OsuMods.DoubleTime) != 0)
            ret.Append("DT");
        if ((mods & OsuMods.Nightcore) != 0)
            ret.Append("NC");
        if ((mods & OsuMods.HardRock) != 0)
            ret.Append("HR");
        if ((mods & OsuMods.SuddenDeath) != 0)
            ret.Append("SD");
        if ((mods & OsuMods.Perfect) != 0)
            ret.Append("PF");
        if ((mods & OsuMods.Flashlight) != 0)
            ret.Append("FL");
        
        if (ret.Length == 0)
            ret.Append("None");
        
        return ret.ToString();
    }
}