using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using Prometheus;
using Serilog;

namespace BanchoMultiplayerBot.Bancho
{
    public class CommandHandler : ICommandHandler
    {
        private readonly IMessageHandler _messageHandler;
        private int _lastSpamFilterCount = 0;

        private readonly Dictionary<string, List<QueuedCommand>> _queuedCommands = [];

        private static readonly Counter SentCommandsCounter = Metrics.CreateCounter("bot_commands_sent", "The amount of commands attempted to be executed");
        private static readonly Counter CommandsFailedToSendCounter = Metrics.CreateCounter("bot_commands_message_failed", "The amount of commands that failed to send their message");
        private static readonly Counter CommandsMessageRetriedCounter = Metrics.CreateCounter("bot_commands_message_retried", "The amount of commands that resent the message due to timeout");

        public CommandHandler(IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
            _messageHandler.OnMessageReceived += OnMessageReceived;
        }

        public Task<bool> ExecuteAsync<T>(string channel, IReadOnlyList<string>? args = null) where T : IBanchoCommand
        {
            var task = Task.Run(async () =>
            {
                Log.Verbose("CommandHandler: Executing command {Command} in {Channel}", T.Command, channel);

                if (!_queuedCommands.ContainsKey(channel))
                {
                    _queuedCommands[channel] = [];
                }

                async Task SendMessage()
                {
                    _queuedCommands[channel].Add(new QueuedCommand()
                    {
                        Command = T.Command,
                        SuccessfulResponses = T.SuccessfulResponses,
                        DateTime = DateTime.UtcNow
                    });

                    var cookie = _messageHandler.SendMessageTracked(channel, GetCommandString(T.Command, args, T.AppendSpamFilter));

                    SentCommandsCounter.Inc();

                    // Wait for the message to be sent
                    int timeout = 0;
                    while (!cookie.IsSent)
                    {
                        if (timeout++ > 10)
                        {
                            Log.Error("CommandHandler: Failed to send command {Command} to {Channel}, send timeout reached", T.Command, channel);

                            CommandsFailedToSendCounter.Inc();

                            break;
                        }

                        await Task.Delay(1000);
                    }
                }

                // Send the command
                await SendMessage();

                int timeout = 0;
                int attempts = 0;

                while (!_queuedCommands[channel].Any(x => x.Command == T.Command && x.Responded))
                {
                    // If the command has not been responded to in 5 seconds, resend the command
                    if (timeout++ > 50)
                    {
                        attempts++;
                        timeout = 0;

                        await SendMessage();

                        if (attempts > 5)
                        {
                            _queuedCommands[channel].RemoveAll(x => x.Command == T.Command);

                            CommandsMessageRetriedCounter.Inc();

                            Log.Error("CommandHandler: Failed to execute command {Command} in {Channel}, response timeout reached", T.Command, channel);

                            return false;
                        }
                    }

                    await Task.Delay(1000);
                }

                _queuedCommands[channel].RemoveAll(x => x.Command == T.Command);

                // We aren't responsible for parsing the response, just that it was received
                // so we are done here, and can return true.

                return true;
            });

            return task;
        }

        private void OnMessageReceived(BanchoSharp.Interfaces.IPrivateIrcMessage msg)
        {
            if (msg.IsDirect || !msg.IsBanchoBotMessage)
            {
                return;
            }

            if (!_queuedCommands.TryGetValue(msg.Recipient, out var commands))
            {
                return;
            }

            foreach (var command in commands)
            {
                if (command.Responded)
                {
                    continue;
                }

                foreach (var response in command.SuccessfulResponses)
                {
                    switch (response.Type)
                    {
                        case CommandResponseType.Exact:
                            if (msg.Content == response.Message)
                            {
                                command.Responded = true;
                            }

                            break;
                        case CommandResponseType.StartsWith:
                            if (msg.Content.StartsWith(response.Message))
                            {
                                command.Responded = true;
                            }

                            break;
                        case CommandResponseType.Contains:
                            if (msg.Content.Contains(response.Message))
                            {
                                command.Responded = true;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string GetCommandString(string command, IReadOnlyList<string>? args, bool includeSpamFilter)
        {
            var commandString = command;

            if (args != null)
            {
                commandString += " " + string.Join(" ", args);
            }

            if (includeSpamFilter)
            {
                commandString += " " + new string('\u200B', _lastSpamFilterCount);

                if (_lastSpamFilterCount++ > 4)
                {
                    _lastSpamFilterCount = 0;
                }
            }

            return commandString;
        }
    }
}
