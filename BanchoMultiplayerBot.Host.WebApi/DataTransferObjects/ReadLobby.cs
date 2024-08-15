namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class ReadLobby
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public int PlayerCount { get; set; }

    public IEnumerable<ReadPlayer>? Players { get; set; } = null;

    public ReadPlayer? Host { get; set; } = null;
}