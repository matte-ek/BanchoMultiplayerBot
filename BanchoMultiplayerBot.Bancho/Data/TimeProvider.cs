using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Data;

public class TimeProvider : ITimeProvider
{
    public DateTime UtcNow { get; } = DateTime.UtcNow;
}