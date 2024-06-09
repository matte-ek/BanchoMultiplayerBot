using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Interfaces
{
    /// <summary>
    /// A player message, could be a private message or a message sent in a channel
    /// </summary>
    public interface IPlayerMessage : IPrivateIrcMessage
    {
        /// <summary>
        /// Replies to the message in the same channel it was sent
        /// </summary>
        public void Reply(string message);
    }
}
