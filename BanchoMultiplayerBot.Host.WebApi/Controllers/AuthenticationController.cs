using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

    [HttpGet("osu-callback")]
    public async Task<ActionResult> OsuResponse()
    {
        var result = await HttpContext.AuthenticateAsync("osu");

        if (!result.Succeeded || result.Principal == null)
        {
            throw new UnauthorizedAccessException();
        }

        // TODO: Add user administrator check
        
        var claims = result.Principal.Identities.First().Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(claimsIdentity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/");
    }   
    
     [HttpGet("validate")]
     [Authorize]
     public ActionResult Validate()
     {
         return Ok();
     }
}