namespace BanchoMultiplayerBot.Events;

[AttributeUsage(AttributeTargets.Method)]
public class BotEvent(BotEventType type) : Attribute
{
    public BotEventType Type { get; init; } = type;
}

public enum BotEventType
{
    BotStarted,
    BotStopped,
    MessageReceived,
    MessageSent,
    LobbyMessageReceived
}