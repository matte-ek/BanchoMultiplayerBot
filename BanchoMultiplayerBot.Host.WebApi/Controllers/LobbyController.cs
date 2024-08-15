using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LobbyController(Bot bot) : ControllerBase
{
    [HttpGet("list")]
    public async IAsyncEnumerable<ReadLobbyList> ListLobbies()
    {
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
            
            yield return readLobbyList;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> Get(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return NotFound();
        }

        var config = await lobby.GetLobbyConfiguration();

        return Ok(new ReadLobby()
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
            }
        });
    }

    [HttpGet("{id:int}/config")]
    public async Task<ActionResult<LobbyConfiguration>> GetConfig(int id)
    {
        var lobby = bot.Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == id);

        if (lobby == null)
        {
            return NotFound();
        }

        return Ok(await lobby.GetLobbyConfiguration());
    }

    [HttpPut("{id:int}/config")]
    public async Task<ActionResult> UpdateConfig(int id, LobbyConfiguration newConfiguration)
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

        return Ok();
    }
}