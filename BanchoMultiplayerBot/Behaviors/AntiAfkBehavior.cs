using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviors;

public class AntiAfkBehavior(BehaviorEventContext context) : IBehavior
{
    private const int AfkTimerSeconds = 30;

    [BotEvent(BotEventType.TimerElapsed, "AfkTimer")]
    public void OnAfkTimerElapsed()
    {
        if (context.MultiplayerLobby.Host == null)
        {
            return;
        }

        if (context.MultiplayerLobby.Players.Count == 0)
        {
            return;
        }

        context.Lobby.BanchoConnection.MessageHandler.SendMessage("BanchoBot", $"!stat {context.MultiplayerLobby.Host.Name}");
        context.Lobby.TimerProvider?.FindOrCreateTimer("AfkTimer").Start(TimeSpan.FromSeconds(AfkTimerSeconds));
    }

    [BanchoEvent(BanchoEventType.BanchoBotMessageReceived)]
    public void OnMessageReceived(IPrivateIrcMessage message)
    {
        if (!message.Content.StartsWith("Stats for (") || context.MultiplayerLobby.Host == null)
        {
            return;
        }

        // Parse player name from message
        var playerNameBegin = message.Content.IndexOf('(') + 1;
        var playerNameEnd = message.Content.IndexOf(')');
        var playerName = message.Content[playerNameBegin..playerNameEnd];

        if (context.MultiplayerLobby.Players.Count == 0 || context.MultiplayerLobby.Host.Name != playerName)
        {
            return;
        }

        if (!message.Content.Contains("is Afk"))
        {
            return;
        }
        
        context.VoteProvider.FindOrCreateVote("SkipVote", "Skip the host").Abort();

        context.SendMessage($"!mp kick {context.GetPlayerIdentifier(context.MultiplayerLobby.Host.Name)}");
    }

    [BanchoEvent(BanchoEventType.HostChanged)]
    public void OnHostChanged() => context.Lobby.TimerProvider?.FindOrCreateTimer("AfkTimer").Start(TimeSpan.FromSeconds(AfkTimerSeconds));

    [BanchoEvent(BanchoEventType.HostChangingMap)]
    public void OnHostChangingMap() => context.Lobby.TimerProvider?.FindOrCreateTimer("AfkTimer").Stop();

    [BanchoEvent(BanchoEventType.MatchStarted)]
    public void OnMatchStarted() => context.Lobby.TimerProvider?.FindOrCreateTimer("AfkTimer").Stop();

}