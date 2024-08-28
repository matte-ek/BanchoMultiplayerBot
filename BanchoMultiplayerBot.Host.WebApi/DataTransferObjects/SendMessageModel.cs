namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class SendMessageModel
{
    public int LobbyId { get; set; }
    public string Content { get; set; } = string.Empty;
}