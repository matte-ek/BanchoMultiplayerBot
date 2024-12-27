using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class PlayerBanRepository : BaseRepository<PlayerBan>
{
    public async Task CreateBan(User user, bool hostBan, string? reason, DateTime? expire)
    {
        var ban = new PlayerBan
        {
            UserId = user.Id,
            HostBan = hostBan,
            Reason = reason,
            Expire = expire,
            Time = DateTime.UtcNow
        };

        await AddAsync(ban);
    }

    public async Task RemoveBan(PlayerBan ban)
    {
        var entity = await BotDbContext.PlayerBans.FirstOrDefaultAsync(x => x.Id == ban.Id);

        if (entity == null)
        {
            return;
        }

        entity.Active = false;
    }
    
    public async Task<IReadOnlyList<PlayerBan>> GetActiveBans()
    {
        return await BotDbContext.PlayerBans
            .Where(x => x.Active && (x.Expire == null || x.Expire > DateTime.UtcNow))
            .Include(x => x.User)
            .AsNoTracking()
            .ToListAsync();
    }
}