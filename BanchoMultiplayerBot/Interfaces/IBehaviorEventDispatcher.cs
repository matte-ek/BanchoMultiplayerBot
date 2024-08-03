namespace BanchoMultiplayerBot.Interfaces;

public interface IBehaviorEventDispatcher
{
    public void RegisterBehavior(string behavior);

    public void Start();
    public void Stop();

    public Task OnBehaviorEvent(object param);
    public Task OnTimerElapsed(ITimer timer);
}