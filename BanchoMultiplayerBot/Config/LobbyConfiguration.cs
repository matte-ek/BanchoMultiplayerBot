using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Config;

public class LobbyConfiguration
{
    
    // General multiplayer lobby details

    public string Name { get; set; } = string.Empty;

    public GameMode? Mode { get; set; } = GameMode.osu;

    public string[]? SelectedMods { get; set; }

    public int? Size { get; set; } = 16;

    public string? Password { get; set; } = string.Empty;

    public bool? AutomaticallySkipAfterViolations { get; set; } = false;
    public int? ViolationSkipCount { get; set; } = 3;
    
    // Bot specific configuration

    public string[]? Behaviours { get; set; }
    
    public bool LimitStarRating { get; set; } = false;

    public float MinimumStarRating { get; set; }
    public float MaximumStarRating { get; set; }

    // Bot will still display the set star rating limits, but will
    // allow a specified margin.
    public float? StarRatingErrorMargin { get; set; }
    
    public bool LimitMapLength { get; set; } = false;

    public int MinimumMapLength { get; set; }
    public int MaximumMapLength { get; set; }

    // Behaviour specific 

    public string? PreviousQueue;
    
}