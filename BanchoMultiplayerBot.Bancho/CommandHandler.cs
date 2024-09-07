using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Bancho.Interfaces;
using Prometheus;
using Serilog;
using TimeProvider = BanchoMultiplayerBot.Bancho.Data.TimeProvider;

namespace BanchoMultiplayerBot.Bancho
{
    public class CommandHandler : ICommandHandler
    {
        private readonly IMessageHandler _messageHandler;
        private readonly ITimeProvider _timeProvider;
        
        private int _lastSpamFilterCount;

        private readonly Dictionary<string, List<QueuedCommand>> _queuedCommands = [];

        private readonly object _queuedCommandsLock = new();
        private readonly object _spamFilterLock = new();

        private readonly BanchoClientConfiguration _banchoClientConfiguration;

        private static readonly Counter SentCommandsCounter = Metrics.CreateCounter("bot_commands_sent", "The amount of commands attempted to be executed");
        private static readonly Counter CommandsFailedToSendCounter = Metrics.CreateCounter("bot_commands_message_failed", "The amount of commands that failed to send their message");
        private static readonly Counter CommandsMessageRetriedCounter = Metrics.CreateCounter("bot_commands_message_retried", "The amount of commands that resent the message due to timeout");

        public CommandHandler(IMessageHandler messageHandler, BanchoClientConfiguration banchoClientConfiguration, ITimeProvider? timeProvider = null)
        {
            _banchoClientConfiguration = banchoClientConfiguration;
            _timeProvider = timeProvider ?? new TimeProvider();
            _messageHandler = messageHandler;
            _messageHandler.OnMessageReceived += OnMessageReceived;
        }

        public Task<bool> ExecuteAsync<T>(string channel, IReadOnlyList<string>? args = null) where T : IBanchoCommand
        {
            var task = Task.Run(async () =>
            {
                Log.Verbose("CommandHandler: Executing command {Command} in {Channel}", T.Command, channel);

                lock (_queuedCommandsLock)
                {
                    if (!_queuedCommands.ContainsKey(channel))
                    {
                        _queuedCommands[channel] = [];
                    }
                }

                async Task SendMessage()
                {
                    lock (_queuedCommandsLock)
                    {
                        _queuedCommands[channel].Add(new QueuedCommand
                        {
                            Command = T.Command,
                            SuccessfulResponses = T.SuccessfulResponses,
                            DateTime = _timeProvider.UtcNow
                        });
                    }

                    var cookie = _messageHandler.SendMessageTracked(channel, GetCommandString(T.Command, args, T.AppendSpamFilter));
                    var messageSendTimeout = _timeProvider.UtcNow.AddSeconds(5);

                    SentCommandsCounter.Inc();
                    
                    // Wait for the message to be sent
                    while (!cookie.IsSent)
                    {
                        if (_timeProvider.UtcNow > messageSendTimeout)
                        {
                            Log.Error("CommandHandler: Failed to send command {Command} to {Channel}, send timeout reached", T.Command, channel);

                            CommandsFailedToSendCounter.Inc();

                            break;
                        }

                        await Task.Delay(50);
                    }
                }

                // Send the command
                await SendMessage();

                var executeTimeout = _timeProvider.UtcNow.AddSeconds(_banchoClientConfiguration.BanchoCommandTimeout);
                
                int executionAttempts = 0;
                
                while (true)
                {
                    lock (_queuedCommandsLock)
                    {
                        if (_queuedCommands[channel].Any(x => x.Command == T.Command && x.Responded))
                        {
                            break;
                        }
                    }
                    
                    // If the command has not been responded to in 5 seconds, resend the command
                    if (_timeProvider.UtcNow > executeTimeout)
                    {
                        executionAttempts++;
                        executeTimeout = _timeProvider.UtcNow.AddSeconds(_banchoClientConfiguration.BanchoCommandTimeout);

                        await SendMessage();

                        if (executionAttempts > _banchoClientConfiguration.BanchoCommandAttempts)
                        {
                            lock (_queuedCommandsLock)
                            {
                                _queuedCommands[channel].RemoveAll(x => x.Command == T.Command);
                            }
                            
                            CommandsMessageRetriedCounter.Inc();

                            Log.Error("CommandHandler: Failed to execute command {Command} in {Channel}, response timeout reached", T.Command, channel);

                            return false;
                        }
                    }

                    await Task.Delay(50);
                }

                lock (_queuedCommandsLock)
                {
                    _queuedCommands[channel].Remove(_queuedCommands[channel].First(x => x.Command == T.Command));
                }

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

            lock (_queuedCommandsLock)
            {
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
        }

        private string GetCommandString(string command, IReadOnlyList<string>? args, bool includeSpamFilter)
        {
            var commandString = command;

            if (args != null)
            {
                commandString += " " + string.Join(" ", args);
            }

            if (!includeSpamFilter)
            {
                return commandString;
            }

            lock (_spamFilterLock)
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
