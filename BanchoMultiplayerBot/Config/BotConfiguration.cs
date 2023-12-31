using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Config;

public class BotConfiguration
{
 
    // osu! authentication
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    public bool? IsBotAccount { get; set; } = false;

    // osu! API authentication
    public string ApiKey { get; set; } = null!;
    
    // Presets lobby configuration the bot will attempt to create
    // upon the bot starting.
    public LobbyConfiguration[]? LobbyConfigurations { get; set; }

    // Global configuration applied to all managed lobbies
    public bool? AutoStartAllPlayersReady { get; set; }

    public bool? EnableAutoStartTimer { get; set; }
    public int? AutoStartTimerTime { get; set; }

    public Announcement[]? Announcements { get; set; }
    
    public bool? EnableWebhookNotifications { get; set; }
    public string? WebhookUrl { get; set; }
    public bool? WebhookNotifyLobbyTerminations { get; set; } = true;
    public bool? WebhookNotifyConnectionErrors { get; set; } = true;
    
    public string? StatisticsUrl { get; set; }

}