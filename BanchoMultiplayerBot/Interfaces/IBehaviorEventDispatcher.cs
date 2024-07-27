namespace BanchoMultiplayerBot.Interfaces;

public interface IBehaviorEventDispatcher
{
    public ILobby Lobby { get; init; }

    public void RegisterBehavior(IBehavior behavior);

    public void Start();
    public void Stop();
}