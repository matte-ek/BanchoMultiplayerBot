using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class MapBanRepository : IDisposable
{
    private readonly BotDbContext _botDbContext;
    private bool _disposed;

    public MapBanRepository()
    {
        _botDbContext = new BotDbContext();
    }

    public async Task AddMapBan(int? beatmapSetId, int? beatmapId)
    {
        if (beatmapId == null && beatmapSetId == null)
        {
            return;
        }
        
        var mapBan = new MapBan()
        {
            BeatmapSetId = beatmapSetId,
            BeatmapId = beatmapId
        };

        await _botDbContext.AddAsync(mapBan);
        await _botDbContext.SaveChangesAsync();
    }

    public async Task<bool> IsMapBanned(int? beatmapSetId, int? beatmapId)
    {
        if (beatmapId == null && beatmapSetId == null)
        {
            return false;
        }
        
        return await _botDbContext.MapBans
            .Where(x => x.BeatmapSetId == beatmapSetId || x.BeatmapId == beatmapId)
            .AnyAsync();
    }

    public async Task<IReadOnlyList<MapBan>> GetAll()
    {
        return await _botDbContext.MapBans.AsNoTracking().ToListAsync();
    }

    public async Task RemoveAsync(MapBan mapBan)
    {
        var entity = await _botDbContext.MapBans.FirstOrDefaultAsync(x => x.Id == mapBan.Id);

        if (entity == null)
        {
            return;
        }

        _botDbContext.Remove(entity);
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