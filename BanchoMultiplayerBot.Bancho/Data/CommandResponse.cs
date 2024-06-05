namespace BanchoMultiplayerBot.Bancho.Data
{
    public enum CommandResponseType
    {
        Exact,
        StartsWith,
        Contains,
    }

    public class CommandResponse
    {
        public string Message { get; set; } = string.Empty;

        public CommandResponseType Type { get; set; } = CommandResponseType.Exact;
    }
}
