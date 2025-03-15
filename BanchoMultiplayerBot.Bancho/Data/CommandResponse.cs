namespace BanchoMultiplayerBot.Bancho.Data
{
    public enum CommandResponseType
    {
        Exact,
        StartsWith,
        Contains,
    }

    /// <summary>
    /// Data on how Bancho should respond to a command, we use this
    /// to make sure commands are executed properly.
    /// </summary>
    public class CommandResponse
    {
        public string Message { get; set; } = string.Empty;

        public CommandResponseType Type { get; set; } = CommandResponseType.Exact;
    }
}
