namespace BanchoMultiplayerBot.Bancho.Data
{
    /// <summary>
    /// A message that is queued to be sent to Bancho.
    /// </summary>
    internal class QueuedMessage
    {
        /// <summary>
        /// The bancho channel to send the message to
        /// </summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// The messages contents
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// When the message was created, does not mean it was sent.
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the message was sent to Bancho
        /// </summary>
        public DateTime Sent { get; set; }

        /// <summary>
        /// If this message is tracked, we store a cookie to track it.
        /// </summary>
        public TrackedMessageCookie? TrackCookie { get; set; } = null;
    }
}
