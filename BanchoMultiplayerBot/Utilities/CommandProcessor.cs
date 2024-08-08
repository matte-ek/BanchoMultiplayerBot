using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Utilities;

public class CommandProcessor(Bot bot)
{
    private readonly List<IPlayerCommand> _commands = [];

    public void Start()
    {
        RegisterCommands();

        bot.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;
    }

    public void Stop()
    {
        bot.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;

        _commands.Clear();
    }

    private void RegisterCommands()
    {
        var commands = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IPlayerCommand).IsAssignableFrom(p) && p.IsClass)
            .ToList();

        foreach (var commandType in commands)
        {
            var command = Activator.CreateInstance(commandType);

            if (command == null)
            {
                Log.Warning("CommandProcessor: Failed to create instance of command {CommandType}", commandType);
                continue;
            }

            Log.Verbose("CommandProcessor: Registered command {CommandType}", commandType);

            _commands.Add((IPlayerCommand)command);
        }
    }

    private async void OnMessageReceived(IPrivateIrcMessage message)
    {
        if (message.IsBanchoBotMessage || !message.Content.StartsWith('!'))
        {
            return;
        }

        var args = message.Content.Split(' ');
        var command =
            _commands.FirstOrDefault(x => x.Command == args[0][1..] || x.Aliases?.Contains(args[0][1..]) == true);

        if (command == null)
        {
            return;
        }

        // Make sure the command is allowed to be executed "globally"
        // if the message is a direct message to the bot
        if (!command.AllowGlobal && message.IsDirect)
        {
            return;
        }

        using var userRepo = new UserRepository();
        var user = await userRepo.FindOrCreateUser(message.Sender);

        // Make sure the user is allowed to execute the command
        if (command.Administrator && !user.Administrator)
        {
            return;
        }

        // Make sure the minimum amount of arguments is met
        if (command.MinimumArguments > 0)
        {
            if (args.Length - 1 < command.MinimumArguments)
            {
                if (command.Usage != null)
                {
                    var channel = message.IsDirect ? message.Sender : message.Recipient;
                    
                    bot.BanchoConnection.MessageHandler.SendMessage(channel, $"Usage: {command.Usage}");;
                }
                
                return;
            }
        }
        
        var commandContext = new CommandEventContext(message, args.Skip(1).ToArray(), command, user, bot.BanchoConnection.MessageHandler);

        // Execute the command in a global context
        await command.ExecuteAsync(commandContext);

        if (message.IsDirect)
        {
            return;
        }

        // Execute the command in the context of a multiplayer lobby
        foreach (var lobby in bot.Lobbies)
        {
            if (lobby.MultiplayerLobby == null ||
                lobby.MultiplayerLobby.ChannelName != message.Recipient ||
                lobby.BehaviorEventProcessor == null)
            {
                continue;
            }

            commandContext.Lobby = lobby;
            commandContext.Player = lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == message.Sender.ToIrcNameFormat());
            
            await lobby.BehaviorEventProcessor.OnCommandExecuted(command.Command, commandContext);
        }
    }
}