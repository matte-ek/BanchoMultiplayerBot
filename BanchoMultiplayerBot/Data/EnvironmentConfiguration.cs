namespace BanchoMultiplayerBot.Data;

public class EnvironmentConfiguration
{
    public string OsuUsername { get; set; } = null!;
    public string OsuPassword { get; set; } = null!;

    /// <summary>
    /// Whether the account is in the bot group on osu!
    /// This will adjust the rate limit.
    /// </summary>
    public bool? OsuIsBotAccount { get; set; } = false;
    
    public string OsuApiKey { get; set; } = null!;
} 