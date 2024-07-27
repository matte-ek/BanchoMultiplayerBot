namespace BanchoMultiplayerBot.Events;

[AttributeUsage(AttributeTargets.Method)]
public class CommandEvent(string command) : Attribute
{
    public string Command { get; init; } = command;
}