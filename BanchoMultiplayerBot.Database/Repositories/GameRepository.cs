using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories
{
    public class GameRepository : BaseRepository<Game>
    {
        public async Task<Game?> GetLatestGameByMapIdAsync(int mapId)
        {
            return await BotDbContext.Games
                .Where(x => x.BeatmapId == mapId)
                .LastOrDefaultAsync();
        }
        
        public async Task<int> GetGameCountByMapIdAsync(int mapId, DateTime? ageLimit)
        {
            if (ageLimit != null)
            {
                return await BotDbContext.Games
                    .Where(x => x.BeatmapId == mapId && x.Time >= ageLimit)
                    .CountAsync();
            }
            
            return await BotDbContext.Games
                .Where(x => x.BeatmapId == mapId)
                .CountAsync();
        }

        public async Task<IReadOnlyList<Game>> GetRecentGames(int mapId, int count = 5)
        {
            return await BotDbContext.Games
                .Where(x => x.BeatmapId == mapId)
                .OrderByDescending(x => x.Time)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetGamesCount()
        {
            return await BotDbContext.Games.CountAsync();
        }
    }
}