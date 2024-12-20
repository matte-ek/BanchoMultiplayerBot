using Serilog;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

/// <summary>
/// This service is just a fail-safe, in case the bot is not responding or is not working as expected.
/// If the bot is not connected and/or not responding, we will just exit the bot (which will trigger a restart).
/// </summary>
public class BotHealthService(Bot bot, IHostApplicationLifetime applicationLifetime)
{
    private bool _isRunning;
    private Task? _botWatchdogTask;

    private int _unconnectedSeconds;
    
    public void Start()
    {
        _isRunning = true;
        
        _botWatchdogTask = Task.Run(BotWatchdogTask);       
    }

    public async Task Stop()
    {
        _isRunning = false;

        if (_botWatchdogTask != null)
        {
            await Task.WhenAny(_botWatchdogTask, Task.Delay(TimeSpan.FromSeconds(1)));
        }
    }

    private async Task BotWatchdogTask()
    {
        while (_isRunning)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            
            // Check if the bot is connected to Bancho
            if (!bot.BanchoConnection.IsConnected)
            {
                _unconnectedSeconds++;

                if (_unconnectedSeconds <= 60)
                {
                    continue;
                }
                
                Log.Fatal("Bot is not connected to Bancho for more than 60 seconds, stopping the bot.");
                    
                // If we're not connected for more than 60 seconds, we should just quit the bot
                // as something has probably gone wrong.
                applicationLifetime.StopApplication();
            }
            else
            {
                _unconnectedSeconds = 0;
            }
            
            // TODO: Possibly also check if the bot is receiving messages, etc.
        }
    }
}