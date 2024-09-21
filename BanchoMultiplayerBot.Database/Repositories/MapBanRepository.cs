using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories;

public class MapBanRepository : BaseRepository<MapBan>
{
    public async Task<bool> IsMapBanned(int? beatmapSetId, int? beatmapId)
    {
        if (beatmapId == null && beatmapSetId == null)
        {
            return false;
        }
        
        return await BotDbContext.MapBans
            .Where(x => x.BeatmapSetId == beatmapSetId || x.BeatmapId == beatmapId)
            .AnyAsync();
    }
}