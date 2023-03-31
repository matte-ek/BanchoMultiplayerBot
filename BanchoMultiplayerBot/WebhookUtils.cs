using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Serilog;

namespace BanchoMultiplayerBot
{
    /// <summary>
    /// Quick utility to send webhooks
    /// </summary>
    internal class WebhookUtils
    {
        private static readonly HttpClient _httpClient = new();

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
                await _httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
            }
            catch (Exception e)
            {
                Log.Error($"Failed to send webhook, {e.Message}");
            }
        }
    }
}
