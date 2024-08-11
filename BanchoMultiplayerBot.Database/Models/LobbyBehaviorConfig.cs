using System.ComponentModel.DataAnnotations;

namespace BanchoMultiplayerBot.Database.Models;

public class LobbyBehaviorConfig
{
    public int Id { get; set; }
    
    [Required]
    public int LobbyConfigurationId { get; set; }

    [Required]
    public string BehaviorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Dynamic JSON data for behaviour
    /// </summary>
    public string Data { get; set; } = string.Empty;
}