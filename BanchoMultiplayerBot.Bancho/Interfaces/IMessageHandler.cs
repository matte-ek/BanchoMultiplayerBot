using BanchoMultiplayerBot.Bancho.Data;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IMessageHandler
    {
        /// <summary>
        /// Whetever the internal message pump is running or not.
        /// </summary>
        public bool IsRunning { get; }

        public event Action<IPrivateIrcMessage>? OnMessageReceived;
        public event Action<IPrivateIrcMessage>? OnMessageSent;

        /// <summary>
        /// Starts the internal message pump
        /// </summary>
        public void Start();

        /// <summary>
        /// Stops the internal message pump, if necessary. 
        /// Will be blocking until task is closed, should be instantaneous.
        /// </summary>
        public void Stop();

        /// <summary>
        /// Sends a message to a channel with rate limiting and whatnot
        /// </summary>
        /// <param name="channel">The bancho channel or username</param>
        /// <param name="message">The message contents, max 300 characters</param>
        public void SendMessage(string channel, string message);

        /// <summary>
        /// Allows you to send a message to a channel and track if the message has been sent or not.
        /// </summary>
        /// <param name="channel">The bancho channel or username</param>
        /// <param name="message">The message contents, max 300 characters</param>
        /// <returns>Tracking cookie</returns>
        public TrackedMessageCookie SendMessageTracked(string channel, string message);
    }
}
