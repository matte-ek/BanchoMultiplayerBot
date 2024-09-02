using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController(IConfiguration configuration) : ControllerBase
{
    [HttpGet("osu")]
    public ActionResult OsuLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = configuration["Bot:FrontendUrl"]!,
        };

        return Challenge(properties, "osu");
    }

    [HttpGet("validate")]
    [Authorize]
    public ActionResult Validate()
    {
        var username = HttpContext.User.Claims.First(x => x.Type == ClaimTypes.Name)!.Value;
        var id = HttpContext.User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier)!.Value;

        return Ok(new UserIdentity()
        {
            Id = id,
            Username = username
        });
    }

    private class UserIdentity
    {
        public string Id { get; set; }
        public string Username { get; set; }
    }
}