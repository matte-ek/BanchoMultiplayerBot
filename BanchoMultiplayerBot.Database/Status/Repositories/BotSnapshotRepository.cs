using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BanchoMultiplayerBot.Database.Status.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Status.Repositories
{
    public class BotSnapshotRepository : IDisposable
    {
        private readonly StatusDbContext _statusDbContext;
        private bool _disposed;

        public BotSnapshotRepository()
        {
            _statusDbContext = new StatusDbContext();
        }

        public async Task AddSnapshot(BotSnapshot botSnapshot)
        {
            await _statusDbContext.AddAsync(botSnapshot);
            await _statusDbContext.SaveChangesAsync();
        }

        public async Task<bool> RemoveSnapshot(BotSnapshot snapshot)
        {
            var snapshotDatabaseItem = await _statusDbContext.BotSnapshots.Where(x => x.Id == snapshot.Id).Include(x => x.LobbySnapshots).FirstOrDefaultAsync();

            if (snapshotDatabaseItem == null)
            {
                return false;
            }

            _statusDbContext.Remove(snapshotDatabaseItem);

            await _statusDbContext.SaveChangesAsync();

            return true;
        }

        public async Task RemoveSnapshotsPastTime(DateTime time)
        {
            var snapshots = await _statusDbContext.BotSnapshots.Where(x => time >= x.Time).ToListAsync();

            // Removing each one separately will most likely be faster for this scenario.
            foreach (var snapshot in snapshots)
                _statusDbContext.Remove(snapshot);

            await _statusDbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<BotSnapshot>> GetSnapshots()
        {
            return await _statusDbContext.BotSnapshots.Include(x => x.LobbySnapshots).AsNoTracking().ToListAsync();
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
                    _statusDbContext.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
