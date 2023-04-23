using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Host.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace BanchoMultiplayerBot.Host.Web.Extra
{
    /// <summary>
    /// This is a gimmick to show the lobbies status as an embeddable image.
    /// It's more or less currently hardcoded to show less or equal to 3 lobbies, depends on the input SVG
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BannerController : Controller
    {
        private readonly BotService _bot;
        private readonly BannerCacheService _bannerCacheService;
        private readonly HttpClient _httpClient = new();

        public BannerController(BotService bot, BannerCacheService bannerCacheService)
        {
            _bot = bot;
            _bannerCacheService = bannerCacheService;
        }

        [HttpGet("image")]
        public async Task<ActionResult> GetImage()
        {
            // These should theoretically overwrite each other, but I had some issues with caching.
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
            Response.Headers.Add("Surrogate-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
            Response.Headers.Add("Expires", "0");

            try
            {
                if ((DateTime.Now - _bannerCacheService.CacheUpdateTime).TotalSeconds <= 30)
                {
                    return File(Encoding.ASCII.GetBytes(_bannerCacheService.OutputCache), "image/svg+xml");
                }

                var svgFile = await System.IO.File.ReadAllTextAsync("banner.svg");
                var bannerDownloadList = new List<Task<BeatmapCoverData>>();

                int index = 0;
                foreach (var lobby in _bot.Lobbies)
                {
                    svgFile = svgFile.Replace($"$NAME{index}", HttpUtility.HtmlEncode(lobby.Configuration.Name));
                    svgFile = svgFile.Replace($"$PLAYERS{index}", $"{lobby.MultiplayerLobby.Players.Count:00}/{lobby.Configuration.Size}");

                    var behaviour = (MapManagerBehaviour?)lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
                    if (behaviour != null)
                    {
                        var mapName = behaviour.CurrentBeatmapName;

                        if (mapName.Length > 40)
                            mapName = mapName[..40] + "...";

                        svgFile = svgFile.Replace($"$MAP{index}", $"{HttpUtility.HtmlEncode(mapName)}");

                        if (behaviour.CurrentBeatmapSetId != 0)
                        {
                            if (_bannerCacheService.BeatmapCoverCache[index]?.Id == behaviour.CurrentBeatmapSetId)
                            {
                                svgFile = svgFile.Replace($"$BANNER{index}", $"data:image/png;base64, {_bannerCacheService.BeatmapCoverCache[index]?.Data}");
                            }
                            else
                            {
                                int lobbyIndex = index;

                                bannerDownloadList.Add(
                                    Task.Run(async () =>
                                    {
                                        var bannerImage = await _httpClient.GetByteArrayAsync(
                                            $"https://assets.ppy.sh/beatmaps/{behaviour.CurrentBeatmapSetId}/covers/cover.jpg");

                                        return new BeatmapCoverData()
                                        {
                                            Id = behaviour.CurrentBeatmapSetId,
                                            LobbyIndex = lobbyIndex,
                                            Data = Convert.ToBase64String(bannerImage),
                                        };
                                    })
                                );
                            }
                        }
                    }

                    index++;
                }

                var results = await Task.WhenAll(bannerDownloadList);

                foreach (var imgResult in results)
                {
                    _bannerCacheService.BeatmapCoverCache[imgResult.LobbyIndex] = imgResult;

                    svgFile = svgFile.Replace($"$BANNER{imgResult.LobbyIndex}", $"data:image/png;base64, {imgResult.Data}");
                }

                _bannerCacheService.CacheUpdateTime = DateTime.Now;
                _bannerCacheService.OutputCache = svgFile;

                return File(Encoding.ASCII.GetBytes(_bannerCacheService.OutputCache), "image/svg+xml");
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }
    }
}
