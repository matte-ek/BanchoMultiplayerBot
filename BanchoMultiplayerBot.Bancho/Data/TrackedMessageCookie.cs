namespace BanchoMultiplayerBot.Bancho.Data
{
    public class TrackedMessageCookie
    {
        public bool MessageSent { get; set; } = false;

        public DateTime SentTime { get; set; }
    }
}
