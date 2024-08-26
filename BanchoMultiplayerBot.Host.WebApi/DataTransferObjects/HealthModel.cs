namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class HealthModel
{
    /// <summary>
    /// If the bot is properly configured
    /// </summary>
    public bool HasConfigurationError { get; set; } = false;
    
    /// <summary>
    /// If the bot currently is connected to Bancho
    /// </summary>
    public bool IsBanchoConnected { get; set; } = false;
    
    /// <summary>
    /// If any lobby is currently active
    /// </summary>
    public bool IsLobbyActive { get; set; } = false;
}