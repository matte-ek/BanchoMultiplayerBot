namespace BanchoMultiplayerBot.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class BanchoEvent(BanchoEventType type) : Attribute
{
    public BanchoEventType Type { get; } = type;
}

public enum BanchoEventType
{
    MatchStarted,
    MatchFinished,
    MatchAborted,
    OnPlayerJoined,
    OnPlayerDisconnected,
    OnHostChanged,
    OnHostChangingMap,
    OnMapChanged,
    OnSettingsUpdated,
    AllPlayersReady
}