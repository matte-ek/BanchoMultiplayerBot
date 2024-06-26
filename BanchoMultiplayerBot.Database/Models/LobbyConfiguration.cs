using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Database.Models;

public class LobbyConfiguration
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public GameMode? Mode { get; set; } = GameMode.osu;

    public LobbyFormat? TeamMode { get; set; } = LobbyFormat.HeadToHead;

    public WinCondition? ScoreMode { get; set; } = WinCondition.Score;

    public string[]? Mods { get; set; }

    public int? Size { get; set; } = 16;

    public string? Password { get; set; } = string.Empty;

    public string[]? Behaviours { get; set; }
}