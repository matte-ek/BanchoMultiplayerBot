namespace BanchoMultiplayerBot.Bancho.Data
{
    /// <summary>
    /// Data to keep track of a message, to indicate if it has been sent or not.
    /// </summary>
    public class TrackedMessageCookie
    {
        public bool IsSent { get; set; }

        public DateTime SentTime { get; set; }
    }
}
