using BanchoMultiplayerBot.Interfaces;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot;

public class Timer(ITimerProvider timerProvider, string name) : ITimer
{
    public string Name { get; init; } = name;
    
    public bool IsActive { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public ITimerProvider TimerProvider { get; } = timerProvider;

    public void Start(TimeSpan duration)
    {
                
    }

    public void Stop()
    {
        
    }
}