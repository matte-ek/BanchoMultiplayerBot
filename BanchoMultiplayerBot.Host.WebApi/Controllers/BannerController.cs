using BanchoMultiplayerBot.Host.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BannerController(BannerService bannerService, Bot bot) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetBanner()
    {
        // These should theoretically overwrite each other, but I had some issues with caching.
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
        Response.Headers.Append("Surrogate-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
        Response.Headers.Append("Expires", "0");

        var banner = await bannerService.GetBanner();
        
        if (banner == null)
        {
            return NotFound();
        }
        
        return Content(banner, "image/svg+xml; charset=utf-8");
    }
    
    [HttpGet("join/{lobbyId:int}")]
    public ActionResult GetJoinLink(int lobbyId)
    {
        if (0 > lobbyId || lobbyId >= bot.Lobbies.Count)
        {
            return BadRequest();
        }

        var lobby = bot.Lobbies[lobbyId];

        if (lobby.MultiplayerLobby == null)
        {
            return BadRequest();
        }
        
        var channelId = bot.BanchoConnection.ChannelHandler.GetChannelId(lobby.MultiplayerLobby!.ChannelName) ?? 0;

        return Redirect( $"osu://mp/{channelId}");
    }    
}