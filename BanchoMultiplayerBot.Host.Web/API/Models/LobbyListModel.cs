namespace BanchoMultiplayerBot.Host.Web.API.Models;

public class LobbyListModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Players { get; set; }
    public int PlayerCapacity { get; set; }
}