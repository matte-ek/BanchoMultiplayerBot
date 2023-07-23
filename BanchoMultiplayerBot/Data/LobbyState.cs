namespace BanchoMultiplayerBot.Data;

public class LobbyState
{

    public string Name { get; init; } = null!;
    public string Channel { get; init; } = null!;

    /// <summary>
    /// Allow the queue to be restored, useful if the bot just needs to restart. This should be moved elsewhere.
    /// </summary>
    public string? Queue { get; init; }

}