using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LobbyController(LobbyService lobbyService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id)
    {
        var lobby = await lobbyService.GetById(id);
        return lobby == null ? NotFound() : Ok(lobby);
    }
    
    [HttpPost("create")]
    public async Task Create([FromBody] CreateLobbyModel request)
    {
        await lobbyService.CreateLobby(request);
    }

    [HttpDelete("{id:int}")]
    public async Task Remove(int id)
    {
        await lobbyService.RemoveLobby(id);
    }
    
    [HttpGet("list")]
    public async Task<IEnumerable<LobbyModel>> ListLobbies()
    {
        return await lobbyService.GetAllLobbies();
    }
    
    [HttpPost("{id:int}/refresh")]
    public async Task Refresh(int id, bool rejoinChannel = false)
    {
        await lobbyService.RefreshLobby(id, rejoinChannel);
    }
    
    [HttpGet("{id:int}/reassign")]
    public async Task ReassignChannel(int id, string channel)
    {
        await lobbyService.ReassignLobbyChannel(id, channel);
    }
    
    [HttpGet("{id:int}/config")]
    public async Task<ActionResult<LobbyConfiguration>> GetConfig(int id)
    {
        var config = await lobbyService.GetConfiguration(id);
        return config == null ? NotFound() : Ok(config);
    }

    [HttpPost("{id:int}/config")]
    public async Task<ActionResult> UpdateConfig(int id, LobbyConfiguration newConfiguration)
    {
        await LobbyService.UpdateConfiguration(id, newConfiguration);
        return Ok();
    }

    [HttpGet("config/list")]
    public async Task<IEnumerable<ConfigurationListModel>> GetConfigurations()
    {
        return await LobbyService.GetAllConfigurations();
    }
}