using BanchoMultiplayerBot.Database.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class BaseRepository<T> : IRepository<T>, IAsyncDisposable where T : class
{
    protected readonly BotDbContext BotDbContext = new();
    
    public async Task<T?> GetAsync(int id)
    {
        return await BotDbContext.Set<T>().FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await BotDbContext.Set<T>().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await BotDbContext.Set<T>().AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await BotDbContext.Set<T>().AddRangeAsync(entities);
    }

    public void Delete(T entity)
    {
        BotDbContext.Set<T>().Remove(entity);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        BotDbContext.Set<T>().RemoveRange(entities);
    }

    public async Task<long> CountAsync()
    {
        return await BotDbContext.Set<T>().CountAsync();
    }

    public async Task SaveAsync()
    {
        await BotDbContext.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await BotDbContext.DisposeAsync();
        
        GC.SuppressFinalize(this);
    }
}