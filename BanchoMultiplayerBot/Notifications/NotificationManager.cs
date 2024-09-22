using BanchoMultiplayerBot.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Notifications;

public class NotificationManager
{
    private readonly List<INotificationProvider> _notificationProviders = [];

    public NotificationManager(IBotConfiguration botConfiguration)
    {
        if (botConfiguration.DiscordWebhookUrl != null)
        {
            _notificationProviders.Add(new DiscordNotificationProvider(botConfiguration.DiscordWebhookUrl));
        }
    }

    public void Notify(string title, string message)
    {
        // Fire and forget, don't want to block/expect await from the caller
        Task.Run(async () =>
        {
            try
            {
                foreach (var provider in _notificationProviders)
                {
                    await provider.NotifyAsync(title, message);
                }
            }
            catch (Exception e)
            {
                Log.Error("NotificationManager: Failed to send notification: {Error}", e.Message);
            }
        });
    }
}