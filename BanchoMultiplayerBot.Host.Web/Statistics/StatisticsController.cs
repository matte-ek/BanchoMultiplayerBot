using BanchoMultiplayerBot.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    [Route("api/[controller]")]
    [ApiController]
    [FeatureGate("StatisticsController")]
    public class StatisticsController : Controller
    {
        private readonly BotService _bot;

        public StatisticsController(BotService bot)
        {
            _bot = bot;
        }

        [HttpGet]
        public Task<ActionResult> Get()
        {
            try
            {
                int gamesPlayed = 0;
                int playerCount = 0;
                int mapViolations = 0;
                int hostSkipCount = 0;
                int gamesAborted = 0;

                var lobbyData = new List<StatisticsLobbyData>();
                
                _bot.Lobbies.ForEach(lobby =>
                {
                    lobbyData.Add(new StatisticsLobbyData()
                    {
                        GamesPlayed = lobby.Statistics.GamesPlayed,
                        GamesAborted = lobby.Statistics.GamesAborted,
                        MapViolations = lobby.Statistics.MapViolationCount,
                        HostSkipCount = lobby.Statistics.HostSkipCount,
                        Players = lobby.MultiplayerLobby.Players.Count,
                    });
                    
                    gamesPlayed += lobby.Statistics.GamesPlayed;
                    gamesAborted += lobby.Statistics.GamesAborted;
                    mapViolations += lobby.Statistics.MapViolationCount;
                    hostSkipCount += lobby.Statistics.HostSkipCount;
                    playerCount += lobby.MultiplayerLobby.Players.Count;
                });
                
                return Task.FromResult<ActionResult>(Ok(new StatisticsSnapshot()
                {
                    IsConnected = _bot.Client.IsConnected && _bot.Client.IsAuthenticated,
                    TotalPlayers = playerCount,
                    MapViolations = mapViolations,
                    HostSkipCount = hostSkipCount,
                    GamesPlayed = gamesPlayed,
                    GamesAborted = gamesAborted,
                    MessagesSent = _bot.RuntimeInfo.MessagesSent,
                    MessagesReceived = _bot.RuntimeInfo.MessagesReceived,
                    TotalApiRequests = _bot.RuntimeInfo.ApiRequests,
                    TotalApiErrors = _bot.RuntimeInfo.ApiErrors,
                    Lobbies = lobbyData
                }));
            }
            catch (Exception e)
            {
                return Task.FromResult<ActionResult>(BadRequest());
            }
        }
    }
}
