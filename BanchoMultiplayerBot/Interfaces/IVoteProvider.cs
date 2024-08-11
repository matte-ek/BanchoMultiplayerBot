namespace BanchoMultiplayerBot.Interfaces;

public interface IVoteProvider
{
    public IVote FindOrCreateVote(string name, string description);

    public Task Start();
    public Task Stop();
}