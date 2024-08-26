namespace BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

public class MessageModel
{
    public int Id { get; set; }

    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; }
    
    public bool IsAdministratorMessage { get; set; }
    public bool IsBanchoMessage { get; set; }
}