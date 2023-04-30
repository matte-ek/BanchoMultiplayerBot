namespace BanchoMultiplayerBot.Data;

public class LobbyState
{

    public string Name { get; init; } = null!;
    public string Channel { get; init; } = null!;

    /// <summary>
    /// Allow the queue to be restored, useful if the bot just needs to restart.
    /// </summary>
    public string? Queue { get; init; } = null!;

}