using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class PlayerBanRepository : IDisposable
{
    private readonly BotDbContext _botDbContext;
    private bool _disposed;

    public PlayerBanRepository()
    {
        _botDbContext = new BotDbContext();
    }

    public async Task CreateBan(User user, bool hostBan, string? reason, DateTime? expire)
    {
        var ban = new PlayerBan()
        {
            UserId = user.Id,
            HostBan = hostBan,
            Reason = reason,
            Expire = expire,
            Time = DateTime.Now
        };

        await _botDbContext.AddAsync(ban);
        await _botDbContext.SaveChangesAsync();
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