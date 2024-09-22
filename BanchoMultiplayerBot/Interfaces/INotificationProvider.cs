namespace BanchoMultiplayerBot.Interfaces;

public interface INotificationProvider
{
    public Task NotifyAsync(string title, string message);
}