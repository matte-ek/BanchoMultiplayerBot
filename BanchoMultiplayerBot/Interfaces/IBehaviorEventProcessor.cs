using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Interfaces;

public interface IBehaviorEventProcessor
{
    /// <summary>
    /// Listen to any external behavior events
    /// </summary>
    public event Action<string>? OnExternalBehaviorEvent;
    
    /// <summary>
    /// Register a new behavior to the processor
    /// </summary>
    /// <param name="behavior">Behavior class name</param>
    public Task RegisterBehavior(string behavior);
    
    /// <summary>
    /// Starts the event listener
    /// </summary>
    public void Start();
    
    /// <summary>
    /// Stops the event listener
    /// </summary>
    public void Stop();
    
    internal Task OnInitializeEvent();
    internal Task OnBehaviorEvent(string name, object? param = null, bool triggerExternalEvent = true);
    internal Task OnCommandExecuted(string command, CommandEventContext commandEventContext);
    internal Task OnTimerElapsed(ITimer timer);
    internal Task OnTimerEarlyWarningElapsed(ITimer timer);
}