namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class ReadLobbyList
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public int PlayerCount { get; set; }
}