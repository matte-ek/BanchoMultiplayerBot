namespace BanchoMultiplayerBot.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BanchoEvent(BanchoEventType type) : Attribute
{
    public BanchoEventType Type { get; } = type;
}

public enum BanchoEventType
{
    MessageReceived,
    BanchoBotMessageReceived,
    MatchStarted,
    MatchFinished,
    MatchAborted,
    PlayerJoined,
    PlayerDisconnected,
    HostChanged,
    HostChangingMap,
    MapChanged,
    SettingsUpdated,
    AllPlayersReady
}