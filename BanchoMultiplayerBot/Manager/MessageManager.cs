using System.Collections.Concurrent;
using BanchoMultiplayerBot.Data;
using Serilog;

namespace BanchoMultiplayerBot.Manager;

public class MessageManager
{
    /// <summary>
    /// Message queue which is part of the message rate limiting system.
    /// </summary>
    private readonly BlockingCollection<QueuedMessage> _messageQueue = new(20);

    /// <summary>
    /// List of messages the message pump will internally ignore, we do this because there is no way
    /// of safely removing messages in the message queue.
    /// </summary>
    private readonly List<Guid> _ignoredMessages = new();

    private readonly Bot _bot;

    public MessageManager(Bot bot)
    {
        _bot = bot;
    }

    public void Start()
    {
        Task.Run(MessagePumpTask);
    }

    /// <summary>
    /// Sends a message to the channel (may also be username), will also handle rate limiting and anti-spam automatically.
    /// </summary>
    public void SendMessage(string channel, string message)
    {
        _messageQueue.Add(new QueuedMessage()
        {
            Channel = channel,
            Content = message
        });
    }

    /// <summary>
    /// Task to send all messages within the queue, and handle rate limits for the messages.
    /// </summary>
    private async Task MessagePumpTask()
    {
        int messageBurstCount = _bot.Configuration.IsBotAccount == true ? 300 : 8;
        int messageAge = _bot.Configuration.IsBotAccount == true ? 60 : 6;
        const int maxMessageLength = 400;

        List<QueuedMessage> sentMessages = new();

        try
        {
            while (true)
            {
                var message = _messageQueue.Take();

                if (_ignoredMessages.Contains(message.Id))
                {
                    _ignoredMessages.Remove(message.Id);
                    continue;
                }
                
                _bot.RuntimeInfo.Statistics.MessageSendQueue.Set(_messageQueue.Count);
                
                bool shouldThrottle;

                do
                {
                    shouldThrottle = sentMessages.Count >= messageBurstCount - 3;

                    // Remove old messages that are more than 5 seconds old
                    sentMessages.RemoveAll(x => (DateTime.Now - x.Time).Seconds > messageAge);

                    if (!shouldThrottle) continue;

                    Thread.Sleep(1000);
                } while (shouldThrottle);

                message.Time = DateTime.Now;

                // Ideally the messages should maybe just get trimmed here and sent anyway, but this isn't really
                // meant as an convenience, it's more of a fail-safe to never exceed the message limit. 
                if (message.Content.Length >= maxMessageLength)
                {
                    Log.Warning($"Ignoring message '{message.Content}', message is too long.");
                    continue;
                }

                Log.Verbose($"Sending message '{message.Content}' from {message.Time} (current queue: {sentMessages.Count})");
                
                try
                {
                    await _bot.Client.SendPrivateMessageAsync(message.Channel, message.Content);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while sending message: {e.Message}");
                }

                sentMessages.Add(message);
            }
        }
        catch (InvalidOperationException)
        {
            // An InvalidOperationException means that Take() was called on a completed collection,
            // so we'll just exit out off this thread normally.
        }
    }

}