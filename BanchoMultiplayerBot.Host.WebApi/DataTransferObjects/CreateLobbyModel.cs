namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class CreateLobbyModel
{
    public string Name { get; set; } = string.Empty;
    
    public int? CopyFromId { get; set; }
    
    public string? PreviousChannel { get; set; }
}