using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories
{
    public class GameRepository : IDisposable
    {
        private readonly BotDbContext _botDbContext;
        private bool _disposed;

        public GameRepository()
        {
            _botDbContext = new BotDbContext();
        }

        public async Task<Game?> GetLatestGameByMapId(int mapId)
        {
            return await _botDbContext.Games
                .Where(x => x.BeatmapId == mapId)
                .LastOrDefaultAsync();
        }
        
        public async Task<int> GetGameCountByMapId(int mapId, DateTime? ageLimit)
        {
            if (ageLimit != null)
            {
                return await _botDbContext.Games
                    .Where(x => x.BeatmapId == mapId && x.Time >= ageLimit)
                    .CountAsync();
            }
            
            return await _botDbContext.Games
                .Where(x => x.BeatmapId == mapId)
                .CountAsync();
        }

        public async Task AddGame(Game game)
        {
            await _botDbContext.AddAsync(game);
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
}