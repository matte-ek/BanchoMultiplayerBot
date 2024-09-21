using System.ComponentModel.DataAnnotations;

namespace BanchoMultiplayerBot.Database.Models;

public class NoticeMessage
{
    public int Id { get; set; }
 
    [MaxLength(512)]
    public string Message { get; set; } = string.Empty;
}