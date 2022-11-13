namespace BanchoMultiplayerBot.Models;

public class BotConfiguration
{
 
    // osu! authentication
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    // Presets lobby configuration the bot will attempt to create
    // upon the bot starting.
    public LobbyConfiguration[]? LobbyConfigurations { get; set; }

    // Global configuration applied to all managed lobbies
    public bool? AutoStartAllPlayersReady { get; set; }

    public bool? EnableAutoStartTimer { get; set; }
    public int? AutoStartTimerTime { get; set; }
    
}