namespace BanchoMultiplayerBot.Events;

[AttributeUsage(AttributeTargets.Method)]
public class BotEvent(BotEventType type, string? optionalParameter = null) : Attribute
{
    public BotEventType Type { get; init; } = type;
    
    public string? OptionalParameter { get; init; } = optionalParameter;
}

public enum BotEventType
{
    BotStarted,
    BotStopped,
    MessageReceived,
    MessageSent,
    LobbyMessageReceived,
    CommandExecuted,
    TimerElapsed,
    BehaviourEvent
}