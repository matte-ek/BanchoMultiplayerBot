using BanchoMultiplayerBot.Interfaces;
using Serilog;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot.Data;

public class Timer(ITimerProvider timerProvider, string name) : ITimer
{
    public string Name { get; init; } = name;
    
    public bool IsActive { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int EarlyWarning { get; set; }
    
    public ITimerProvider TimerProvider { get; } = timerProvider;
    
    public void Start(TimeSpan duration, int earlyWarning = 0)
    {
        Log.Verbose("Timer ({Name}): Starting timer with duration {Duration}", Name, duration);
        
        StartTime = DateTime.UtcNow;
        EndTime = StartTime + duration;
        EarlyWarning = earlyWarning;
        IsActive = true;
    }

    public void Stop()
    {
        Log.Verbose("Timer ({Name}): Stopping timer", Name);
        IsActive = false;
    }
}