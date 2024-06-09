namespace BanchoMultiplayerBot.Configuration
{
    public class BotConfiguration
    {
        // osu! authentication
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;

        public bool? IsBotAccount { get; set; } = false;

        // osu! API authentication
        public string ApiKey { get; set; } = null!;


    }
}
