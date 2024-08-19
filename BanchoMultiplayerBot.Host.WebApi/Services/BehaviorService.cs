using BanchoMultiplayerBot.Database;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class BehaviorService
{
    public async Task<string?> GetBehaviorConfig(int lobbyId, string behaviorName)
    {
        await using var context = new BotDbContext();
        var config = await context.LobbyBehaviorConfig.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName.ToLower()));
        return config?.Data;
    }

    public async Task SetBehaviorConfig(int lobbyId, string behaviorName, string configuration)
    {
        await using var context = new BotDbContext();
        
        var config = await context.LobbyBehaviorConfig.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName));

        if (config == null)
        {
            throw new Exception("Could not find behavior config");
        }
        
        config.Data = configuration;

        await context.SaveChangesAsync();
    }
    
    public async Task<string?> GetBehaviorData(int lobbyId, string behaviorName)
    {
        await using var context = new BotDbContext();
        var config = await context.LobbyBehaviorData.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName));
        return config?.Data;
    }
}