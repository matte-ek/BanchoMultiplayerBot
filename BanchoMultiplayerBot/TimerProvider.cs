using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ITimer = BanchoMultiplayerBot.Interfaces.ITimer;

namespace BanchoMultiplayerBot;

public class TimerProvider(ILobby lobby) : ITimerProvider
{
    private readonly List<ITimer> _timers = [];

    private bool _isRunning;
    private Task? _workerTask;
    
    /// <summary>
    /// Will start the timer provider and begin processing timers.
    /// </summary>
    public async Task Start()
    {
        await LoadTimers();
        
        _isRunning = true;
        _workerTask = Task.Run(TaskWorker);
    }

    /// <summary>
    /// Will stop the timer provider and stop processing timers.
    /// </summary>
    public async Task Stop()
    {
        await SaveTimers();
        
        if (!_isRunning)
        {
            Log.Warning("TimerProvider ({LobbyIndex}): Attempt to stop timer provider while it is not running", lobby.LobbyConfigurationId);
            return;
        }
        
        _isRunning = false;

        if (_workerTask == null)
        {
            Log.Warning("TimerProvider ({LobbyIndex}): Attempt to stop timer provider while worker task is null", lobby.LobbyConfigurationId);
            return;
        }

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            Log.Warning("TimerProvider ({LobbyIndex}): Worker task did not complete within 1 second, something is up", lobby.LobbyConfigurationId);
        }
    }
    
    /// <summary>
    /// Finds or creates a timer with the specified name.
    /// </summary>
    public ITimer FindOrCreateTimer(string name)
    {
        var timer = _timers.FirstOrDefault(t => t.Name == name);

        if (timer != null)
        {
            return timer;
        }
        
        timer = new Timer(this, name);
        
        _timers.Add(timer);

        return timer;
    }
    
    /// <summary>
    /// Loads existing timers from the database.
    /// </summary>
    private async Task LoadTimers()
    {
        await using var context = new BotDbContext();

        var timers = await context.LobbyTimers.Where(x => x.LobbyId == lobby.LobbyConfigurationId).ToListAsync();

        foreach (var timer in timers)
        {
            var newTimer = new Timer(this, timer.Name)
            {
                IsActive = timer.IsActive,
                StartTime = timer.StartTime,
                EndTime = timer.EndTime
            };
            
            if ((DateTime.UtcNow - timer.EndTime).TotalSeconds > 45)
            {
                // If more than 45 seconds have passed since the timer ended, we'll just mark it as inactive
                // as the relevant event probably shouldn't be triggered anymore
                newTimer.IsActive = false;
                
                Log.Warning("TimerProvider ({LobbyIndex}): Timer {Name} was marked as inactive as more than 45 seconds have passed since it ended", lobby.LobbyConfigurationId, timer.Name);
            }

            _timers.Add(newTimer);
        }
        
        Log.Verbose("TimerProvider ({LobbyIndex}): Loaded {Count} timer(s) from database", lobby.LobbyConfigurationId , timers.Count);
    }

    /// <summary>
    /// Saves the current timers to the database.
    /// </summary>
    private async Task SaveTimers()
    {
        await using var context = new BotDbContext();

        foreach (var timer in _timers)
        {
            var existingTimerModel = await context.LobbyTimers.FirstOrDefaultAsync(x => x.LobbyId == lobby.LobbyConfigurationId && x.Name == timer.Name);
            
            if (existingTimerModel == null)
            {
                var newTimerModel = new LobbyTimer()
                {
                    Name = timer.Name,
                    LobbyId = lobby.LobbyConfigurationId,
                    StartTime = timer.StartTime,
                    EndTime = timer.EndTime,
                    IsActive = timer.IsActive
                };
                
                context.LobbyTimers.Add(newTimerModel);
            }
            else
            {
                existingTimerModel.StartTime = timer.StartTime;
                existingTimerModel.EndTime = timer.EndTime;
                existingTimerModel.IsActive = timer.IsActive;
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// The worker task that will process timers, and trigger events when they elapse.
    /// </summary>
    private async Task TaskWorker()
    {
        Log.Verbose("TimerProvider ({LobbyIndex}): Worker task for lobby started", lobby.LobbyConfigurationId);
        
        while (_isRunning)
        {
            await Task.Delay(1000);

            foreach (var timer in _timers.Where(timer => timer.IsActive && DateTime.UtcNow >= timer.EndTime))
            {
                Log.Verbose("TimerProvider ({LobbyIndex}): Timer {Name} elapsed", lobby.LobbyConfigurationId, timer.Name);
                
                timer.IsActive = false;

                if (lobby.BehaviorEventDispatcher == null)
                {
                    Log.Warning("TimerProvider ({LobbyIndex}): BehaviorEventDispatcher is null, cannot trigger timer elapsed event", lobby.LobbyConfigurationId);
                    
                    continue;
                }
                
                await lobby.BehaviorEventDispatcher.OnTimerElapsed(timer);
            }
        }
        
        Log.Verbose("TimerProvider ({LobbyIndex}): Worker task stopped", lobby.LobbyConfigurationId);
    }
}