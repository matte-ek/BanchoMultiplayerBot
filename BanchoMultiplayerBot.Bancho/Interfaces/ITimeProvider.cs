namespace BanchoMultiplayerBot.Bancho.Interfaces;

public interface ITimeProvider
{
    public DateTime UtcNow { get; }
}