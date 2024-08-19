using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class LobbyService(Bot bot)
{
    public async Task<IReadOnlyList<ReadLobbyList>> GetAllLobbies()
    {
        List<ReadLobbyList> lobbies = [];
        
        foreach (var lobby in bot.Lobbies)
        {
            var config = await lobby.GetLobbyConfiguration();

            var readLobbyList = new ReadLobbyList()
            {
                Id = lobby.LobbyConfigurationId,
                IsActive = lobby.IsReady,
                Name = config.Name,
                PlayerCount = lobby.MultiplayerLobby?.Players.Count ?? 0
            };
            
            lobbies.Add(readLobbyList);
        }

        return lobbies;
    }
 
    public async Task<ReadLobby?> GetById(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return null;
        }

        var config = await lobby.GetLobbyConfiguration();

        return new ReadLobby()
        {
            Id = lobby.LobbyConfigurationId,
            IsActive = lobby.IsReady,
            Name = config.Name,
            PlayerCount = lobby.MultiplayerLobby?.Players.Count ?? 0,
            Players = lobby.MultiplayerLobby?.Players.Select(x => new ReadPlayer()
            {
                Name = x.Name,
                OsuId = x.Id
            }),
            Host = lobby.MultiplayerLobby?.Host == null ? null : new ReadPlayer()
            {
                Name = lobby.MultiplayerLobby!.Host.Name,
                OsuId = lobby.MultiplayerLobby?.Host.Id
            },
            Behaviors = config.Behaviours
        };
    }
    
    public async Task<LobbyConfiguration?> GetLobbyConfiguration(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return null;
        }

        return await lobby.GetLobbyConfiguration();
    }
    
    public async Task UpdateLobbyConfiguration(int id, LobbyConfiguration newConfiguration)
    {
        await using var context = new BotDbContext();

        var configuration = await context.LobbyConfigurations.FirstOrDefaultAsync(x => x.Id == id);
        if (configuration == null)
        {
            throw new InvalidOperationException("Failed to find lobby configuration.");
        }

        configuration.Name = newConfiguration.Name;
        configuration.Password = newConfiguration.Password;
        configuration.Size = newConfiguration.Size;
        configuration.Mode = newConfiguration.Mode;
        configuration.TeamMode = newConfiguration.TeamMode;
        configuration.ScoreMode = newConfiguration.ScoreMode;
        configuration.Mods = newConfiguration.Mods;
        configuration.Behaviours = newConfiguration.Behaviours;
        
        await context.SaveChangesAsync();
    }
}