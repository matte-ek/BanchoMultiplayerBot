using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories
{
    public class UserRepository : IDisposable
    {
        private readonly BotDbContext _botDbContext;
        private bool _disposed;

        public UserRepository()
        {
            _botDbContext = new BotDbContext();
        }

        public async Task<User> FindOrCreateUser(string username)
        {
            return await FindUser(username) ?? await CreateUser(username);
        }
        
        public async Task<User?> FindUser(string username)
        {
            return await _botDbContext.Users.Where(x => x.Name == username)
                .Include(x => x.Bans)
                .FirstOrDefaultAsync();
        }

        public async Task<User> CreateUser(string username)
        {
            var user = new User()
            {
                Id = Guid.NewGuid(),
                Name = username
            };

            await _botDbContext.AddAsync(user);
            await _botDbContext.SaveChangesAsync();

            return user;
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
