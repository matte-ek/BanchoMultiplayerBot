using MudBlazor;

namespace BanchoMultiplayerBot.Host.Web.Models
{
    public class MessageModel
    {
        public DateTime Time { get; set; }
        public string Author { get; set; } = null!;
        public string Content { get; set; } = null!;
        public Color Color { get; set; }
    }
}
