namespace BanchoMultiplayerBot.Behaviors.Config;

public class AutoStartBehaviorConfig
{
    /// <summary>
    /// Whether the bot should automatically start the match when all players are ready.
    /// </summary>
    public bool AllPlayersReadyStart { get; set; } = true;
    
    /// <summary>
    /// The number of seconds to wait before starting the match when a new valid map is selected.
    /// </summary>
    public int NewMapTimer { get; set; } = 90;
    
    /// <summary>
    /// The number of seconds before the main timer runs out to warn players that the match is about to start.
    /// </summary>
    public int StartEarlyWarning { get; set; } = 10;

    public int StartTimerMinimumSeconds { get; set; } = 1;
    public int StartTimerMaximumSeconds { get; set; } = 500;
}