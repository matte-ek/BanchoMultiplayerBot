namespace BanchoMultiplayerBot.Interfaces;

public interface ITimer
{
    public string Name { get; }
    
    public bool IsActive { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public ITimerProvider TimerProvider { get; }
    
    public void Start(TimeSpan duration);
    
    public void Stop();
}