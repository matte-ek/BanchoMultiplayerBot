using System.Text;
using BanchoMultiplayerBot.Interfaces;
using Newtonsoft.Json;

namespace BanchoMultiplayerBot.Notifications;

public class DiscordNotificationProvider(string targetUrl) : INotificationProvider
{
    public Task NotifyAsync(string title, string message) => SendDiscordNotification(targetUrl, title, message);
    
    private static async Task SendDiscordNotification(string url, string title, string message)
    {
        using var httpClient = new HttpClient();
        
        var data = new
        {
            embeds = new List<object>
            {
                new
                { 
                    title,
                    description = message,
                    color = 0x3e97e6
                }
            }
        };

        await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
    }
}