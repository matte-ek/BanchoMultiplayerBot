using System.Reflection;
using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Dispatcher for behavior events, allowing for the registration of behavior classes and methods to be executed
/// </summary>
public class BehaviorEventProcessor(ILobby lobby) : IBehaviorEventProcessor
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
            var ignoreArgs = method.GetParameters().Length == 0;

            // Check if the method has the BanchoEvent attribute, if so, add it to the list of events
            // that will be executed when the event is triggered
            var banchoEventAttribute = method.GetCustomAttribute<BanchoEvent>();
            if (banchoEventAttribute != null)
            {
                _events.Add(new BanchoBehaviorEvent(behavior, method, behaviorType, ignoreArgs, banchoEventAttribute.Type));
            }
            
            // Check if the method has the BanchoEvent attribute, if so, add it to the list of events
            // that will be executed when the event is triggered
            var botEventAttribute = method.GetCustomAttribute<BotEvent>();
            if (botEventAttribute != null)
            {
                _events.Add(new BotBehaviorEvent(behavior, method, behaviorType, ignoreArgs, botEventAttribute.Type, botEventAttribute.OptionalScope));
            }
        }

        Log.Verbose("BehaviorEventDispatcher: Registered behaviour {BehaviorName} successfully", behavior);
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
        lobby.MultiplayerLobby.OnAllPlayersReady += OnAllPlayersReady;
        lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
        lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;

        // Register other stuff
        lobby.BanchoConnection.MessageHandler.OnMessageReceived += OnMessageReceived;
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
        lobby.MultiplayerLobby.OnAllPlayersReady -= OnAllPlayersReady;
        lobby.MultiplayerLobby.OnBeatmapChanged -= OnBeatmapChanged;
        lobby.MultiplayerLobby.OnSettingsUpdated -= OnSettingsUpdated;

        // Unregister other stuff
        lobby.BanchoConnection.MessageHandler.OnMessageReceived -= OnMessageReceived;

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

    private async void OnAllPlayersReady()
    {
        await ExecuteBanchoCallback(BanchoEventType.AllPlayersReady);
    }

    private async void OnSettingsUpdated()
    {
        await ExecuteBanchoCallback(BanchoEventType.OnSettingsUpdated);
    }
    
    private async void OnBeatmapChanged(BeatmapShell beatmapShell)
    {
        await ExecuteBanchoCallback(BanchoEventType.OnMapChanged, beatmapShell);
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

    private async void OnMessageReceived(IPrivateIrcMessage msg)
    {
        await ExecuteBotCallback(BotEventType.MessageReceived, msg);
    }

    public async Task OnInitializeEvent()
    {
        await ExecuteBotCallback(BotEventType.Initialize);
    }
    
    public async Task OnBehaviorEvent(string name, object? param = null)
    {
        await ExecuteBotCallbackScoped(BotEventType.BehaviourEvent, name, param);
    }

    public async Task OnCommandExecuted(string command, CommandEventContext commandEventContext)
    {
        await ExecuteBotCallbackScoped(BotEventType.CommandExecuted, command, commandEventContext);
    }
    
    public async Task OnTimerElapsed(ITimer timer)
    {
        await ExecuteBotCallbackScoped(BotEventType.TimerElapsed, timer.Name, timer);
    }
    
    public async Task OnTimerEarlyWarningElapsed(ITimer timer)
    {
        await ExecuteBotCallbackScoped(BotEventType.TimerEarlyWarning, timer.Name, timer);
    }
    
    private async Task ExecuteBotCallbackScoped(BotEventType botEventType, string scope, object? param = null)
    {
        var banchoEvents = _events
            .OfType<BotBehaviorEvent>()
            .Where(x => x.BotEventType == botEventType && x.OptionalScope?.ToLower() == scope.ToLower());

        await ExecuteCallback(banchoEvents.ToList(), param);
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
            var instance = Activator.CreateInstance(behaviorEvent.BehaviorType, new BehaviorEventContext(lobby, _cancellationTokenSource!.Token)) as IBehavior;

            try
            {
                Log.Verbose("BehaviorEventDispatcher: Executing {CallbackName}.{MethodName}() ...", behaviorEvent.Name, behaviorEvent.Method.Name);

                // Invoke the method on the behavior class instance
                var methodTask = behaviorEvent.Method.Invoke(instance, behaviorEvent.IgnoreArguments ? [] : [param]);

                // If we have a return value, it's a task, so await it
                if (methodTask != null)
                {
                    await (Task)methodTask;
                }

                // If the behavior class implements IBehaviorDataConsumer, save the data
                if (instance is IBehaviorDataConsumer dataBehavior)
                {
                    await dataBehavior.SaveData();
                }
            }
            catch (Exception e)
            {
                Log.Error("BehaviorEventDispatcher: Exception while executing callback {CallbackName}.{MethodName}(), {Exception}", behaviorEvent.Name, behaviorEvent.Method.Name, e);
            }
        }
    }

    private abstract class BehaviorEvent(string name, MethodInfo method, Type behaviorType, bool ignoreArgs)
    {
        public string Name { get; } = name;
        
        public MethodInfo Method { get; } = method;

        public bool IgnoreArguments { get; } = ignoreArgs;
        
        public Type BehaviorType { get; } = behaviorType;
    }

    private class BanchoBehaviorEvent(string name, MethodInfo method, Type behaviorType, bool ignoreArgs, BanchoEventType banchoEventType) : BehaviorEvent(name, method, behaviorType, ignoreArgs)
    {
        public BanchoEventType BanchoEventType { get; } = banchoEventType;
    }
    
    private class BotBehaviorEvent(string name, MethodInfo method, Type behaviorType, bool ignoreArgs, BotEventType botEventType, string? optionalScope) : BehaviorEvent(name, method, behaviorType, ignoreArgs)
    {
        public BotEventType BotEventType { get; } = botEventType;

        public string? OptionalScope { get; } = optionalScope;
    }
}