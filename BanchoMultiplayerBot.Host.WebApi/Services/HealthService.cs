using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class HealthService(Bot bot)
{
    public HealthModel GetHealth()
    {
        return new HealthModel()
        {
            HasConfigurationError = false, // TODO: Decide if I actually want this.
            IsBanchoConnected = bot.BanchoConnection.IsConnected,
            IsLobbyActive = bot.Lobbies.Any(x => x.Health == LobbyHealth.Ok || x.Health == LobbyHealth.Idle)
        };
    }
}