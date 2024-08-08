namespace BanchoMultiplayerBot.Interfaces;

public interface IVoteProvider
{
    public IVote FindOrCreateVote(string name);

    public Task Start();
    public Task Stop();
}