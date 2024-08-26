using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(HealthService healthService) : ControllerBase
{
    [HttpGet]
    public HealthModel Get()
    {
        return healthService.GetHealth();
    }
}