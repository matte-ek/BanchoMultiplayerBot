namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class ReadPlayer
{
    public int? OsuId { get; set; } = null;

    public string Name { get; set; } = null;
}