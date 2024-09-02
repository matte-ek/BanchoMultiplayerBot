namespace BanchoMultiplayerBot.Bancho.Data
{
    public class BanchoClientConfiguration
    {
        /// <summary>
        /// osu! IRC username, obtained from https://osu.ppy.sh/home/account/edit#legacy-api
        /// </summary>
        public string Username { get; init; } = string.Empty;
        
        /// <summary>
        /// osu! IRC password, obtained from https://osu.ppy.sh/home/account/edit#legacy-api
        /// </summary>
        public string Password { get; init; } = string.Empty;

        /// <summary>
        /// The maximum amount of messages within the rate limit window
        /// </summary>
        public int MessageRateLimitCount { get; init; } = 8;
        
        /// <summary>
        /// The rate limit window in seconds
        /// </summary>
        public int MessageRateLimitWindow { get; init; } = 6;
        
        /// <summary>
        /// The amount of time to wait before reconnecting to Bancho after a disconnect
        /// </summary>
        public int BanchoReconnectDelay { get; init; } = 30;
        
        /// <summary>
        /// The amount of times to attempt to reconnect to Bancho before giving up
        /// </summary>
        public int BanchoReconnectAttempts { get; init; } = 5;
        
        /// <summary>
        /// The amount of time to wait before attempting to reconnect to Bancho
        /// </summary>
        public int BanchoReconnectAttemptDelay { get; init; } = 10;
        
        /// <summary>
        /// The amount of seconds to wait for a bancho command to get executed
        /// </summary>
        public int BanchoCommandTimeout { get; init; } = 5;
        
        /// <summary>
        /// The amount of times to try to execute a bancho command upon failure.
        /// </summary>
        public int BanchoCommandAttempts { get; init; } = 5;
    }
}
