using BanchoMultiplayerBot.Osu.Models;

namespace BanchoMultiplayerBot.Extensions;

public static class BeatmapModelExtensions
{
    /// <summary>
    /// Converts the approved index returned within the API to human readable map staus form.
    /// </summary>
    public static string GetStatusString(this BeatmapModel model)
    {
        return model.Approved switch
        {
            "-2" => "Graveyard",
            "-1" => "WIP",
            "0" => "Pending",
            "1" => "Ranked",
            "3" => "Qualifed",
            "4" => "Loved",
            _ => "Unknown",
        };
    }
}