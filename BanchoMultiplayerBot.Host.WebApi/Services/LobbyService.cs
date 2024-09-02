using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Multiplayer;
using Microsoft.EntityFrameworkCore;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class LobbyService(Bot bot)
{
    public async Task<LobbyExtendedModel?> GetById(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return null;
        }

        var config = await lobby.GetLobbyConfiguration();
        var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(lobby);

        return new LobbyExtendedModel()
        {
            Id = lobby.LobbyConfigurationId,
            Beatmap = mapManagerDataProvider.Data.BeatmapInfo,
            Health = lobby.Health,
            Name = config.Name,
            PlayerCount = lobby.MultiplayerLobby?.Players.Count ?? 0,
            PlayerCapacity = config.Size ?? 16,
            Players = lobby.MultiplayerLobby?.Players.Select(x => new PlayerModel()
            {
                Name = x.Name,
                OsuId = x.Id
            }),
            Host = lobby.MultiplayerLobby?.Host == null ? null : new PlayerModel()
            {
                Name = lobby.MultiplayerLobby!.Host.Name,
                OsuId = lobby.MultiplayerLobby?.Host.Id
            },
            Behaviors = config.Behaviours
        };
    }

    public async Task CreateLobby(CreateLobbyModel lobby)
    {
        await using var context = new BotDbContext();

        var previousConfig = lobby.CopyFromId != null ? await GetConfiguration(lobby.CopyFromId.Value) : null;
        
        var newConfig = new LobbyConfiguration
        {
            Name = lobby.Name,
            Password = previousConfig?.Password ?? string.Empty,
            Size = previousConfig?.Size ?? 16,
            Mode = previousConfig?.Mode ?? GameMode.osu,
            TeamMode = previousConfig?.TeamMode ?? LobbyFormat.HeadToHead,
            ScoreMode = previousConfig?.ScoreMode ?? WinCondition.Score,
            Mods = previousConfig?.Mods ?? [],
            Behaviours = previousConfig?.Behaviours ?? []
        };
        
        context.Add(newConfig);

        // We intentionally save the context here to get the ID.
        await context.SaveChangesAsync();

        // Add a "fake" previous room instance if provided by the user.
        if (lobby.PreviousChannel != null)
        {
            var instance = new LobbyRoomInstance()
            {
                Channel = lobby.PreviousChannel,
                LobbyConfigurationId = newConfig.Id
            };

            context.Add(instance);
            
            await context.SaveChangesAsync();
        }
        
        await bot.ReloadLobbies();
    }
    
    public async Task RemoveLobby(int lobbyConfigId)
    {
        await bot.DeleteLobby(lobbyConfigId);  
    }
    
    public async Task<IReadOnlyList<LobbyModel>> GetAllLobbies()
    {
        List<LobbyModel> lobbies = [];
        
        foreach (var lobby in bot.Lobbies)
        {
            var config = await lobby.GetLobbyConfiguration();

            var readLobbyList = new LobbyModel()
            {
                Id = lobby.LobbyConfigurationId,
                Health = lobby.Health,
                Name = config.Name,
                PlayerCount = lobby.MultiplayerLobby?.Players.Count ?? 0,
                PlayerCapacity = config.Size ?? 16
            };
            
            lobbies.Add(readLobbyList);
        }
        
        return lobbies;
    }

    public async Task RefreshLobby(int lobbyId, bool rejoinChannel)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == lobbyId);

        if (lobby == null)
        {
            return;
        }
        
        if (!rejoinChannel)
        {
            await lobby.RefreshAsync();
        }
        else
        {
            await lobby.ConnectAsync();
        }
    }

    public async Task ReassignLobbyChannel(int lobbyId, string newChannel)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == lobbyId);

        if (lobby == null)
        {
            return;
        }
        
        await using var context = new BotDbContext();
        
        var instance = new LobbyRoomInstance()
        {
            Channel = $"#mp_{newChannel}",
            LobbyConfigurationId = lobbyId
        };

        context.Add(instance);
            
        await context.SaveChangesAsync();
        await lobby.ConnectAsync();
    }

    public async Task<LobbyConfiguration?> GetConfiguration(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return null;
        }

        return await lobby.GetLobbyConfiguration();
    }
    
    public async Task UpdateConfiguration(int id, LobbyConfiguration newConfiguration)
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
    
    public async Task<IEnumerable<ConfigurationListModel>> GetAllConfigurations()
    {
        await using var context = new BotDbContext();
        
        return await context.LobbyConfigurations.Select(x => new ConfigurationListModel()
        {
            Id = x.Id,
            Name = x.Name
        }).ToListAsync();
    }
}