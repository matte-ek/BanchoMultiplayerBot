using System.Reflection;
using System.Threading.Channels;
using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Prometheus;
using Serilog;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot.Utilities;

/// <summary>
/// Dispatcher for behavior events, allowing for the registration of behavior classes and methods to be executed
/// </summary>
public class BehaviorEventProcessor(ILobby lobby) : IBehaviorEventProcessor
{
    public event Action<string>? OnExternalBehaviorEvent;
    
    private readonly List<BehaviorEvent> _events = [];
    
    private readonly List<string> _registeredBehaviors = [];
    
    private readonly Dictionary<string, Channel<EventExecution>> _eventChannels = new();
    
    private readonly List<Task> _workers = [];
    
    private CancellationTokenSource? _cancellationTokenSource;

    private static readonly Counter EventsExecutedCount = Metrics.CreateCounter("bot_event_processor_execution_count", "The number events executed", ["lobby_index", "event_type"]);
    private static readonly Counter EventsExceptionCount = Metrics.CreateCounter("bot_event_processor_exception_count", "The number exceptions caused by a event", ["lobby_index", "event_type"]);
    private static readonly Histogram EventExecuteTime = Metrics.CreateHistogram("bot_event_processor_execute_time_ms", "The time it took to execute a event", ["lobby_index", "event_type"]);
    
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
        
        // Create at least one instance of each behavior class to ensure that
        // configurations and such as created.
        Activator.CreateInstance(behaviorType, new BehaviorEventContext(lobby, CancellationToken.None));

        _registeredBehaviors.Add(behavior);
        
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
        
        // Create a worker for each behavior
        foreach (var behavior in _registeredBehaviors)
        {
            var channel = Channel.CreateUnbounded<EventExecution>();
            
            _eventChannels.Add(behavior, channel);
            _workers.Add(Task.Run(() => BehaviorEventWorker(channel.Reader, behavior)));
        }
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
        
        // Signal the workers to stop
        foreach (var channel in _eventChannels.Values)
        {
            channel.Writer.Complete();
        }
        
        // Wait for all workers to finish
        Task.WhenAll(_workers).Wait();
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
        await ExecuteBanchoCallback(BanchoEventType.PlayerJoined, player);
    }
    
    private async void OnPlayerDisconnected(PlayerDisconnectedEventArgs eventArgs)
    {
        await ExecuteBanchoCallback(BanchoEventType.PlayerDisconnected, eventArgs.Player);
    }

    private async void OnHostChanged(MultiplayerPlayer player)
    {
        await ExecuteBanchoCallback(BanchoEventType.HostChanged, player);
    }
    
    private async void OnHostChangingMap()
    {
        await ExecuteBanchoCallback(BanchoEventType.HostChangingMap);
    }

    private async void OnAllPlayersReady()
    {
        await ExecuteBanchoCallback(BanchoEventType.AllPlayersReady);
    }

    private async void OnSettingsUpdated()
    {
        await ExecuteBanchoCallback(BanchoEventType.SettingsUpdated);
    }
    
    private async void OnBeatmapChanged(BeatmapShell beatmapShell)
    {
        await ExecuteBanchoCallback(BanchoEventType.MapChanged, beatmapShell);
    }
    
    private async void OnMessageReceived(IPrivateIrcMessage msg)
    {
        if (msg.Recipient != lobby.MultiplayerLobby?.ChannelName)
        {
            if (msg.IsBanchoBotMessage)
            {
                await ExecuteBanchoCallback(BanchoEventType.BanchoBotMessageReceived, msg);
            }
            
            return;
        }
        
        await ExecuteBanchoCallback(BanchoEventType.MessageReceived, msg);
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

    public async Task OnInitializeEvent()
    {
        await ExecuteBotCallback(BotEventType.Initialize);
    }
    
    public async Task OnBehaviorEvent(string name, object? param = null, bool triggerExternalEvent = true)
    {
        if (triggerExternalEvent)
        {
            // Fire of any external listeners, we don't really care about 
            // what they have to say so we don't await them
            _ = Task.Run(() => OnExternalBehaviorEvent?.Invoke(name));
        }
        
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
            var channel = _eventChannels[behaviorEvent.Name];
            
            await channel.Writer.WriteAsync(new EventExecution(behaviorEvent, param));
        }
    }

    private async Task BehaviorEventWorker(ChannelReader<EventExecution> reader, string behaviorName)
    {
        Log.Verbose("BehaviorEventDispatcher: Starting worker for behavior '{Behavior}' ...", behaviorName);

        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var eventExecute))
            {
                var behaviorEvent = eventExecute.BehaviorEvent;
                
                // Create a new instance of the behavior class
                var instance = Activator.CreateInstance(behaviorEvent.BehaviorType, new BehaviorEventContext(lobby, _cancellationTokenSource!.Token)) as IBehavior;

                try
                {
                    //Log.Verbose("BehaviorEventDispatcher: Executing {CallbackName}.{MethodName}() ...", behaviorEvent.Name, behaviorEvent.Method.Name);
                    
                    using var timer = EventExecuteTime.WithLabels(lobby.LobbyConfigurationId.ToString(), behaviorEvent.Name).NewTimer();

                    var eventExecuteItem = eventExecute;
                    
                    var executeEventTask = Task.Run(async () =>
                    {
                        // Invoke the method on the behavior class instance
                        var methodTask = behaviorEvent.Method.Invoke(instance, behaviorEvent.IgnoreArguments ? [] : [eventExecuteItem.Param]);

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
                    });

                    await Task.WhenAny(executeEventTask, Task.Delay(TimeSpan.FromSeconds(15)));
                    
                    if (!executeEventTask.IsCompleted)
                    {
                        Log.Error("BehaviorEventDispatcher: Timeout while executing callback {CallbackName}.{MethodName}()", behaviorEvent.Name, behaviorEvent.Method.Name);
                    }
                
                    EventsExecutedCount.WithLabels(lobby.LobbyConfigurationId.ToString(), behaviorEvent.Name).Inc();
                }
                catch (Exception e)
                {
                    EventsExceptionCount.WithLabels(lobby.LobbyConfigurationId.ToString(), behaviorEvent.Name).Inc();
                    
                    Log.Error("BehaviorEventDispatcher: Exception while executing callback {CallbackName}.{MethodName}(), {Exception}", behaviorEvent.Name, behaviorEvent.Method.Name, e);
                }
            }
        }
        
        Log.Verbose("BehaviorEventDispatcher: Stopped worker for behavior '{Behavior}' ...", behaviorName);
    }

    private record EventExecution(BehaviorEvent BehaviorEvent, object? Param);

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