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

    public async Task RemoveBan(PlayerBan ban)
    {
        var entity = await _botDbContext.PlayerBans.FirstOrDefaultAsync(x => x.Id == ban.Id);

        if (entity == null)
        {
            return;
        }

        entity.Active = false;
    }
    
    public async Task<IReadOnlyList<PlayerBan>> GetActiveBans()
    {
        return await _botDbContext.PlayerBans
            .Where(x => x.Active && (x.Expire == null || x.Expire > DateTime.Now))
            .Include(x => x.User)
            .AsNoTracking()
            .ToListAsync();
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