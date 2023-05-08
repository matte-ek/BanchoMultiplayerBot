using System.Text;
using Newtonsoft.Json;
using Serilog;

namespace BanchoMultiplayerBot.Utilities
{
    /// <summary>
    /// Quick utility to send webhooks
    /// </summary>
    internal static class WebhookUtils
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task SendWebhookMessage(string url, string title, string message)
        {
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

            try
            {
                await HttpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to send webhook, {e.Message}");
            }
        }
    }
}
