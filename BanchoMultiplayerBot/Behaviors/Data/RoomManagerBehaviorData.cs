namespace BanchoMultiplayerBot.Behaviors.Data;

public class RoomManagerBehaviorData
{

    /// <summary>
    /// Indicates if this lobby instance is new and needs an initial reset.
    /// </summary>
    public bool IsNewInstance { get; set; }
    
    /// <summary>
    /// The amount of players that has finished the current match.
    /// </summary>
    public int PlayerFinishCount { get; set; }
    
}

