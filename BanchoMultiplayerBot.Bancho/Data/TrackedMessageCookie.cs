namespace BanchoMultiplayerBot.Bancho.Data
{
    public class TrackedMessageCookie
    {
        public bool IsSent { get; set; } = false;

        public DateTime SentTime { get; set; }
    }
}
