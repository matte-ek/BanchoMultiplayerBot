using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Models;

public class LobbyConfiguration
{
    
    // General multiplayer lobby details

    public string Name { get; set; } = string.Empty;

    public GameMode? Mode { get; set; }

    public string Mods { get; set; } = string.Empty;

    public int? Size { get; set; } = 16;

    public string? Password { get; set; } = string.Empty;
    
    // Bot specific configuration

    public bool LimitStarRating { get; set; } = false;

    public float MinimumStarRating { get; set; }
    public float MaximumStarRating { get; set; }

    public bool LimitMapLength { get; set; } = false;

    public int MinimumMapLength { get; set; }
    public int MaximumMapLength { get; set; }
    
}