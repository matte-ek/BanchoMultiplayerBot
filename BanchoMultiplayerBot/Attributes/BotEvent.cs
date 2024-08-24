namespace BanchoMultiplayerBot.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BotEvent(BotEventType type, string? optionalScope = null) : Attribute
{
    public BotEventType Type { get; init; } = type;
    
    public string? OptionalScope { get; init; } = optionalScope;
}

public enum BotEventType
{
    Initialize,
    MessageReceived,
    MessageSent,
    LobbyMessageReceived,
    CommandExecuted,
    TimerEarlyWarning,
    TimerElapsed,
    BehaviourEvent
}