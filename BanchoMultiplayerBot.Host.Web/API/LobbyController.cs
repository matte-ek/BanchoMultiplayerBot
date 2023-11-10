using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Host.Web.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace BanchoMultiplayerBot.Host.Web.API;

[Route("[controller]")]
[ApiController]
public class LobbyController : Controller
{
    private readonly BotService _bot;
 
    public LobbyController(BotService bot)
    {
        _bot = bot;
    }

    [HttpGet("list")]
    public IEnumerable<LobbyListModel> GetList()
    {
        return _bot.Lobbies.Select(botLobby => new LobbyListModel()
        {
            Id = botLobby.LobbyIndex,
            Name = botLobby.Configuration.Name,
            Players = botLobby.MultiplayerLobby.Players.Count,
            PlayerCapacity = botLobby.Configuration.Size!.Value
        });
    }

    [HttpGet("{id:int}")]
    public ActionResult<LobbyModel> GetLobby(int id)
    {
        var lobby = _bot.Lobbies.FirstOrDefault(x => x.LobbyIndex == id);

        if (lobby == null)
        {
            return BadRequest();
        }

        if (lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour)) is not MapManagerBehaviour mapManagerBehaviour)
        {
            return BadRequest();
        }
        
        return new LobbyModel()
        {
            Id = id,
            Name = lobby.Configuration.Name,
            Configuration = lobby.Configuration,
            BeatmapInfo = mapManagerBehaviour.CurrentBeatmap,
            Players = lobby.MultiplayerLobby.Players.Select(x => new PlayerModel()
            {
                Id = 0,
                Name = x.Name,
                IsHost = lobby.MultiplayerLobby.Host is not null && lobby.MultiplayerLobby.Host == x,
                OsuId = x.Id
            })
        };
    }
}