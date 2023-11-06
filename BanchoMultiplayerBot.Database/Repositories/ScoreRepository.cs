using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class ScoreRepository : IDisposable
{
    private readonly BotDbContext _botDbContext;
    private bool _disposed;

    public ScoreRepository()
    {
        _botDbContext = new BotDbContext();
    }

    public async Task Add(Score score)
    {
        score.Time = DateTime.Now;
        
        await _botDbContext.Scores.AddAsync(score);
    }
    
    public async Task Save()
    {
        await _botDbContext.SaveChangesAsync();
    }

    public async Task<long> GetScoreCount()
    {
        return await _botDbContext.Scores.LongCountAsync();
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