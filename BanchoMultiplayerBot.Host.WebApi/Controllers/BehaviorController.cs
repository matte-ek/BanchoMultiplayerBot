using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]/{lobbyId:int}/{behaviorName}")]
[Authorize]
public class BehaviorController(BehaviorService behaviorService) : ControllerBase
{
    [HttpGet("config")]
    public async Task<string?> GetBehaviorConfig(int lobbyId, string behaviorName)
    {
        return await BehaviorService.GetBehaviorConfig(lobbyId, behaviorName);
    }
    
    [HttpPost("config")]
    public async Task SetBehaviorConfig(int lobbyId, string behaviorName, [FromBody] BehaviorConfigModel configuration)
    {
        await BehaviorService.SetBehaviorConfig(lobbyId, behaviorName, configuration.Configuration);
    }
    
    [HttpGet("data")]
    public async Task<string?> GetBehaviorData(int lobbyId, string behaviorName)
    {
        return await BehaviorService.GetBehaviorData(lobbyId, behaviorName);
    }
}