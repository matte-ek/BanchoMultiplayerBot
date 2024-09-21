using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Database.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        public async Task<User> FindOrCreateUserAsync(string username)
        {
            return await FindUserAsync(username) ?? await CreateUserAsync(username);
        }
        
        public async Task<User?> FindUserAsync(string username)
        {
            return await BotDbContext.Users.Where(x => x.Name == username)
                .Include(x => x.Bans)
                .FirstOrDefaultAsync();
        }

        public async Task<User> CreateUserAsync(string username)
        {
            var user = new User
            {
                Name = username
            };

            await AddAsync(user);

            return user;
        }
    }
}
