namespace BanchoMultiplayerBot.Host.Web.Models;

public class AddPlayerBan
{
    public string Name { get; set; } = string.Empty;
    
    public string? Reason { get; set; } = string.Empty;

    public bool HostBan { get; set; } = true;
    
    public DateTime? Expire { get; set; }
}