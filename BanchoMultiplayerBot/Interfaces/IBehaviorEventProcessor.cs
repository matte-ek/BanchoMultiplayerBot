using BanchoMultiplayerBot.Data;

namespace BanchoMultiplayerBot.Interfaces;

public interface IBehaviorEventProcessor
{
    public void RegisterBehavior(string behavior);

    public void Start();
    public void Stop();

    public Task OnBehaviorEvent(string name, object? param = null);
    public Task OnCommandExecuted(string command, CommandEventContext commandEventContext);
    public Task OnTimerElapsed(ITimer timer);
}