namespace BanchoMultiplayerBot.Interfaces;

public interface ITimer
{
    public string Name { get; }
    
    public bool IsActive { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public ITimerProvider TimerProvider { get; }
    
    public int EarlyWarning { get; set; }
    
    public void Start(TimeSpan duration, int earlyWarning = 0);
    
    public void PostPone(TimeSpan duration);
    
    public void Stop();
}