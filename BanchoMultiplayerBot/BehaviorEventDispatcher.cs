using System.Reflection;
using BanchoMultiplayerBot.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot;

public class BehaviorEventDispatcher(ILobby lobby) : IBehaviorEventDispatcher
{
    private List<BehaviorEvent> _events = [];
    
    public ILobby Lobby { get; init; } = lobby;

    public void RegisterBehavior(IBehavior behavior)
    {

    }

    public void Start()
    {
        if (Lobby.MultiplayerLobby == null)
        {
            throw new InvalidOperationException("BehaviorEventDispatcher: Attempted to start dispatcher while MultiplayerLobby is null.");
        }

        Lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
    }

    public void Stop()
    {

    }

    private void OnMatchStarted()
    {
        var events = _events.Where(x => x.Type == BehaviorEventType.Bancho);

        foreach (var ev in events)
        {
            ev.Method();
        }
    }

    private class BehaviorEvent
    {
        public string Name { get; set; }
        public MethodInfo Method { get; set; }
        public BehaviorEventType Type { get; set; }
    }

    private enum BehaviorEventType
    {
        Bancho,
        Command,
        Bot
    }
}