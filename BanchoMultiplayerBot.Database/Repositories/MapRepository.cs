using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class MapRepository : IDisposable
{
    private readonly BotDbContext _botDbContext;
    private bool _disposed;

    public MapRepository()
    {
        _botDbContext = new BotDbContext();
    }
    
    public async Task<IReadOnlyList<Map>> GetPopularMaps(int count)
    {
        return await _botDbContext.Maps
            .OrderByDescending(x => x.TimesPlayed)
            .Take(count)
            .ToListAsync();
    }
    
    /// <summary>
    /// The map where the least people passed
    /// </summary>
    public async Task<IReadOnlyList<Map>> GetHardestMaps(int count)
    {
        return await _botDbContext.Maps
            .Where(x => x.AverageLeavePercentage != null && x.AveragePassPercentage != null && x.TimesPlayed > 5 && x.AveragePassPercentage > 0)
            .OrderBy(x => x.AveragePassPercentage)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// The map where the most people left
    /// </summary>
    public async Task<IReadOnlyList<Map>> GetBoringMap(int count)
    {
        return await _botDbContext.Maps
            .Where(x => x.AverageLeavePercentage != null && x.AveragePassPercentage != null && x.TimesPlayed > 5 && x.AveragePassPercentage > 0) 
            .OrderBy(x => x.AverageLeavePercentage)
            .Take(count)
            .ToListAsync();
    }

    
    public async Task<int> GetMapCount()
    {
        return await _botDbContext.Maps.CountAsync();
    }

    public async Task Save()
    {
        await _botDbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _botDbContext.Dispose();
            }
        }

        _disposed = true;
    }
}