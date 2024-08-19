using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LobbyController(LobbyService lobbyService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult> Get(int id)
    {
        var lobby = await lobbyService.GetById(id);
        return lobby == null ? NotFound() : Ok(lobby);
    }

    [HttpGet("{id:int}/config")]
    public async Task<ActionResult<LobbyConfiguration>> GetConfig(int id)
    {
        var config = await lobbyService.GetLobbyConfiguration(id);
        return config == null ? NotFound() : Ok(config);
    }

    [HttpPut("{id:int}/config")]
    public async Task<ActionResult> UpdateConfig(int id, LobbyConfiguration newConfiguration)
    {
        await lobbyService.UpdateLobbyConfiguration(id, newConfiguration);
        return Ok();
    }
    
    [HttpGet("list")]
    public async Task<IEnumerable<ReadLobbyList>> ListLobbies()
    {
        return await lobbyService.GetAllLobbies();
    }
}