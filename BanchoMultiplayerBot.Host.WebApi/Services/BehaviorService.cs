using BanchoMultiplayerBot.Database;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

/// <summary>
/// This service handles behavior configurations and data, data is read-only and configurations can be updated.
/// </summary>
public class BehaviorService
{
    public static async Task<string?> GetBehaviorConfig(int lobbyId, string behaviorName)
    {
        await using var context = new BotDbContext();
        var config = await context.LobbyBehaviorConfig.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName.ToLower()));
        return config?.Data;
    }

    public static async Task SetBehaviorConfig(int lobbyId, string behaviorName, string configuration)
    {
        await using var context = new BotDbContext();
        
        var config = await context.LobbyBehaviorConfig.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName.ToLower()));

        if (config == null)
        {
            throw new Exception("Could not find behavior config");
        }
        
        config.Data = configuration;

        await context.SaveChangesAsync();
    }
    
    public static async Task<string?> GetBehaviorData(int lobbyId, string behaviorName)
    {
        await using var context = new BotDbContext();
        var config = await context.LobbyBehaviorData.FirstOrDefaultAsync(x => x.LobbyConfigurationId == lobbyId && x.BehaviorName.ToLower().StartsWith(behaviorName));
        return config?.Data;
    }
}