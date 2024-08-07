using System.Reflection;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Events;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.EventArgs;
using BanchoSharp.Multiplayer;
using Serilog;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot;

/// <summary>
/// Dispatcher for behavior events, allowing for the registration of behavior classes and methods to be executed
/// </summary>
public class BehaviorEventDispatcher(ILobby lobby) : IBehaviorEventDispatcher
{
    private readonly List<BehaviorEvent> _events = [];

    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Register a new component behavior to the dispatcher, using the behavior class name.
    /// </summary>
    /// <param name="behavior">Behavior class name</param>
    /// <exception cref="InvalidOperationException">No behavior found with specified name</exception>
    public void RegisterBehavior(string behavior)
    {
        // Find the behavior type by name, which is a class that implements IBehavior
        var behaviorType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .FirstOrDefault(p => typeof(IBehavior).IsAssignableFrom(p) && p.IsClass && p.Name == behavior);

        if (behaviorType == null)
        {
            Log.Error("BehaviorEventDispatcher: Attempted to register behavior '{Behavior}' which does not exist.", behavior);
            throw new InvalidOperationException($"BehaviorEventDispatcher: Attempted to register behavior '{behavior}' which does not exist.");
        }   
        
        var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            // Check if the method has the BanchoEvent attribute, if so, add it to the list of events
            // that will be executed when the event is triggered
            var banchoEventAttribute = method.GetCustomAttribute<BanchoEvent>();
            if (banchoEventAttribute != null)
            {
                _events.Add(new BanchoBehaviorEvent(behavior, method, behaviorType, banchoEventAttribute.Type));
            }
        }
    }

    /// <summary>
    /// Starts the dispatcher, allowing it to listen for events and execute behavior methods.
    /// </summary>
    /// <exception cref="InvalidOperationException">Multiplayer lobby is null</exception>
    public void Start()
    {
        if (lobby.MultiplayerLobby == null)
        {
            throw new InvalidOperationException("BehaviorEventDispatcher: Attempted to start dispatcher while MultiplayerLobby is null.");
        }
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Register bancho event listeners
        lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        lobby.MultiplayerLobby.OnMatchAborted += OnMatchAborted;
        lobby.MultiplayerLobby.OnPlayerJoined += OnPlayerJoined;
        lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
        lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        lobby.MultiplayerLobby.OnHostChangingMap += OnHostChangingMap;
        lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
    }

    /// <summary>
    /// Stops the dispatcher, preventing it from listening for events, and attempts to cancel any running tasks.
    /// </summary>
    /// <exception cref="InvalidOperationException">Multiplayer lobby is null</exception>
    public void Stop()
    {
        if (lobby.MultiplayerLobby == null)
        {
            throw new InvalidOperationException("BehaviorEventDispatcher: Attempted to stop dispatcher while MultiplayerLobby is null.");
        }
        
        // Unregister bancho event listeners
        lobby.MultiplayerLobby.OnMatchStarted -= OnMatchStarted;
        lobby.MultiplayerLobby.OnMatchFinished -= OnMatchFinished;
        lobby.MultiplayerLobby.OnMatchAborted -= OnMatchAborted;
        lobby.MultiplayerLobby.OnPlayerJoined -= OnPlayerJoined;
        lobby.MultiplayerLobby.OnPlayerDisconnected -= OnPlayerDisconnected;
        lobby.MultiplayerLobby.OnHostChanged -= OnHostChanged;
        lobby.MultiplayerLobby.OnHostChangingMap -= OnHostChangingMap;
        lobby.MultiplayerLobby.OnSettingsUpdated -= OnSettingsUpdated;
        
        // Cancel any running tasks
        _cancellationTokenSource?.Cancel();
    }

    #region Bancho Events Wrappers

    private async void OnMatchStarted()
    {
        await ExecuteBanchoCallback(BanchoEventType.MatchStarted);
    }
    
    private async void OnMatchFinished()
    {
        await ExecuteBanchoCallback(BanchoEventType.MatchFinished);
    }
    
    private async void OnMatchAborted()
    {
        await ExecuteBanchoCallback(BanchoEventType.MatchAborted);
    }

    private async void OnPlayerJoined(MultiplayerPlayer player)
    {
        await ExecuteBanchoCallback(BanchoEventType.OnPlayerJoined, player);
    }
    
    private async void OnPlayerDisconnected(PlayerDisconnectedEventArgs eventArgs)
    {
        await ExecuteBanchoCallback(BanchoEventType.OnPlayerDisconnected, eventArgs.Player);
    }

    private async void OnHostChanged(MultiplayerPlayer player)
    {
        await ExecuteBanchoCallback(BanchoEventType.OnHostChanged, player);
    }
    
    private async void OnHostChangingMap()
    {
        await ExecuteBanchoCallback(BanchoEventType.OnHostChangingMap);
    }
    
    private async void OnSettingsUpdated()
    {
        await ExecuteBanchoCallback(BanchoEventType.OnSettingsUpdated);
    }
    
    private async Task ExecuteBanchoCallback(BanchoEventType banchoEventType, object? param = null)
    {
        var banchoEvents = _events
            .OfType<BanchoBehaviorEvent>()
            .Where(x => x.BanchoEventType == banchoEventType);

        await ExecuteCallback(banchoEvents.ToList(), param);
    }

    #endregion

    #region Bot Events

    public async Task OnBehaviorEvent(object param)
    {
        await ExecuteBotCallback(BotEventType.BehaviourEvent, param);
    }
    
    public async Task OnTimerElapsed(ITimer timer)
    {
        await ExecuteBotCallback(BotEventType.TimerElapsed, timer);
    }
    
    private async Task ExecuteBotCallback(BotEventType botEventType, object? param = null)
    {
        var banchoEvents = _events
            .OfType<BotBehaviorEvent>()
            .Where(x => x.BotEventType == botEventType);

        await ExecuteCallback(banchoEvents.ToList(), param);
    }

    #endregion
    
    private async Task ExecuteCallback<T>(IEnumerable<T> behaviorEvents, object? param = null) where T : BehaviorEvent
    {
        foreach (var behaviorEvent in behaviorEvents)
        {
            // Create a new instance of the behavior class
            var instance = Activator.CreateInstance(behaviorEvent.BehaviorType, new BotEventContext(lobby, _cancellationTokenSource!.Token));

            // Invoke the method on the behavior class instance
            var methodTask = behaviorEvent.Method.Invoke(instance, [param]);
            
            // If we have a return value, it's a task, so await it
            if (methodTask != null)
            {
                await (Task)methodTask;
            }
        }
    }

    private abstract class BehaviorEvent(string name, MethodInfo method, Type behaviorType)
    {
        public string Name { get; set; } = name;
        
        public MethodInfo Method { get; init; } = method;
        
        public Type BehaviorType { get; init; } = behaviorType;
    }

    private class BanchoBehaviorEvent(string name, MethodInfo method, Type behaviorType, BanchoEventType banchoEventType) : BehaviorEvent(name, method, behaviorType)
    {
        public BanchoEventType BanchoEventType { get; init; } = banchoEventType;
    }
    
    private class BotBehaviorEvent(string name, MethodInfo method, Type behaviorType, BotEventType botEventType) : BehaviorEvent(name, method, behaviorType)
    {
        public BotEventType BotEventType { get; init; } = botEventType;
    }
}