using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Interfaces;
using Microsoft.VisualBasic;
using Serilog;
using System.Collections.Concurrent;
using Prometheus;

namespace BanchoMultiplayerBot.Bancho
{
    /// <summary>
    /// Utility class to handle sending and receiving messages from Bancho,
    /// also handles rate limiting automatically.
    /// </summary>
    public class MessageHandler(IBanchoConnection banchoConnection) : IMessageHandler
    {
        public bool IsRunning { get; private set; } = false;

        public event Action<IPrivateIrcMessage>? OnMessageReceived;

        public event Action<IPrivateIrcMessage>? OnMessageSent;

        private readonly IBanchoConnection _banchoConnection = banchoConnection;

        private BlockingCollection<QueuedMessage> _messageQueue = new(20);

        private Task? _messagePumpTask = null;
        private bool _exitRequested = false;

        private static readonly Counter MessagesSentCounter = Metrics.CreateCounter("bot_messages_sent", "Number of messages sent");
        private static readonly Counter MessagesReceivedCounter = Metrics.CreateCounter("bot_messages_received", "Number of messages received");

        public void SendMessage(string channel, string message)
        {
            _messageQueue.Add(new QueuedMessage()
            {
                Channel = channel,
                Content = message
            });
        }

        public TrackedMessageCookie SendMessageTracked(string channel, string message)
        {
            var trackedMessage = new TrackedMessageCookie();

            _messageQueue.Add(new QueuedMessage()
            {
                Channel = channel,
                Content = message,
                TrackCookie = trackedMessage
            });

            return trackedMessage;
        }

        public void Start()
        {
            Log.Verbose("MessageHandler: Starting message pump...");

            // Empty out any previous messages
            _messageQueue.Dispose();
            _messageQueue = new BlockingCollection<QueuedMessage>(20);

            if (_banchoConnection.BanchoClient != null)
            {
                _banchoConnection.BanchoClient.OnPrivateMessageReceived += BanchoOnPrivateMessageReceived;
                _banchoConnection.BanchoClient.OnPrivateMessageSent += BanchoOnPrivateMessageSent;
            }

            // Start the message pump
            _exitRequested = false;
            _messagePumpTask = Task.Run(MessagePumpTask);
        }

        public void Stop()
        {
            Log.Verbose("MessageHandler: Stopping message pump...");

            _exitRequested = true;

            if (_banchoConnection.BanchoClient != null)
            {
                _banchoConnection.BanchoClient.OnPrivateMessageReceived -= BanchoOnPrivateMessageReceived;
                _banchoConnection.BanchoClient.OnPrivateMessageSent -= BanchoOnPrivateMessageSent;
            }

            if (_messagePumpTask == null || _messagePumpTask.Status == TaskStatus.RanToCompletion || _messagePumpTask.Status == TaskStatus.Faulted || _messagePumpTask.Status == TaskStatus.Canceled)
            {
                Log.Warning("MessageHandler: Message pump task is not running during Stop()");
                _messagePumpTask = null;
                return;
            }

            // Since the message pump task is being blocked by the message queue, we'll send a dummy message
            // to make sure the loop starts processing something.
            SendMessage("BanchoBot", "dummy");

            _messagePumpTask?.Wait();
            _messagePumpTask = null;
        }

        private async Task MessagePumpTask()
        {
            const int maxMessageLength = 400;
            const int messageBurstCount = 6;
            const int messageAge = 6;

            IsRunning = true;

            Log.Information("MessageHandler: Started message pump successfully");

            List<QueuedMessage> sentMessages = [];

            while (true)
            {
                var message = _messageQueue.Take();

                if (_exitRequested || _banchoConnection.BanchoClient == null)
                {
                    break;
                }

                bool shouldThrottle;

                do
                {
                    shouldThrottle = sentMessages.Count >= messageBurstCount - 1;

                    // Remove old messages that are more than 5 seconds old
                    sentMessages.RemoveAll(x => (DateTime.UtcNow - x.Sent).TotalSeconds > messageAge);

                    if (!shouldThrottle) continue;

                    Thread.Sleep(1000);
                } while (shouldThrottle);

                message.Sent = DateTime.UtcNow;

                // Maybe trimming the message would be a better idea here, but realistically this shouldn't happen,
                // and this check is more of a fail-safe than anything.
                if (message.Content.Length >= maxMessageLength)
                {
                    continue;
                }

                try
                {
                    await _banchoConnection.BanchoClient.SendPrivateMessageAsync(message.Channel, message.Content);

                    if (message.TrackCookie != null)
                    {
                        message.TrackCookie.MessageSent = true;
                        message.TrackCookie.SentTime = DateTime.UtcNow;
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, "MessageHandler: Failed to send message '{message}' to '{channel}'", message.Content, message.Channel);
                }

                sentMessages.Add(message);
            }

            Log.Verbose("MessageHandler: Message pump has stopped");

            IsRunning = false;
        }

        private void BanchoOnPrivateMessageReceived(IPrivateIrcMessage e)
        {
            Log.Information($"MessageHandler: [{e.Recipient}] {e.Sender}: {e.Content}");

            MessagesReceivedCounter.Inc();

            OnMessageReceived?.Invoke(e);
        }

        private void BanchoOnPrivateMessageSent(IPrivateIrcMessage e)
        {
            Log.Information($"MessageHandler: [{e.Recipient}] {e.Sender}: {e.Content}");

            MessagesSentCounter.Inc();

            OnMessageSent?.Invoke(e);
        }
    }
}
