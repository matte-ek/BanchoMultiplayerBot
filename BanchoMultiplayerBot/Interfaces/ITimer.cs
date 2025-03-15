namespace BanchoMultiplayerBot.Interfaces;

public interface ITimer
{
    /// <summary>
    /// The name to keep track of the timer, and which callback to call
    /// whenever a timer related event occurs.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Indicates whether the timer is currently active or not.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// The time whenever the timer was started.
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// The time whenever the timer should end.
    /// </summary>
    public DateTime EndTime { get; set; }
    
    /// <summary>
    /// This is a mechanism to warn the timer user before the timer ends.
    /// If 0, no warning will be given. Otherwise, the timer can be
    /// set to the amount of seconds before the timer ends to
    /// receive an additional callback before the timer ends.
    /// </summary>
    public int EarlyWarning { get; set; }
    
    public ITimerProvider TimerProvider { get; }
    
    public void Start(TimeSpan duration, int earlyWarning = 0);
    
    public void PostPone(TimeSpan duration);
    
    public void Stop();
}