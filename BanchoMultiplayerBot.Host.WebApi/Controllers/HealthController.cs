using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HealthController(HealthService healthService, IHostApplicationLifetime applicationLifetime, Bot bot) : ControllerBase
{
    [HttpGet]
    public HealthModel Get()
    {
        return healthService.GetHealth();
    }

    [HttpGet("quit")]
    public async Task Quit()
    {
        await bot.StopAsync();
        
        applicationLifetime.StopApplication();
    }
}