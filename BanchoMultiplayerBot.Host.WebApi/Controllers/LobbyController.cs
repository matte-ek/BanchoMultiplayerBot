using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LobbyController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok();
    }
}