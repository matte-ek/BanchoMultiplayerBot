namespace BanchoMultiplayerBot.Interfaces;

public interface ITimerProvider
{
    public ITimer FindOrCreateTimer(string name);

    public Task Start();
    public Task Stop();
}