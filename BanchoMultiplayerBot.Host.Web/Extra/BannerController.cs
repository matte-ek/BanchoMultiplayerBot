using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Host.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Serilog;

namespace BanchoMultiplayerBot.Host.Web.Extra
{
    /// <summary>
    /// This is a gimmick to show the lobbies status as an embeddable image.
    /// It's more or less currently hardcoded to show less or equal to 4 lobbies, depends on the input SVG
    /// </summary>
    [Route("[controller]")]
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

        /// <summary>
        /// Returns an SVG image with the lobby status information
        /// Uses an existing SVG template image and overwrites the dynamic content inside of it
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetImage()
        {
            // These should theoretically overwrite each other, but I had some issues with caching.
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, proxy-revalidate";
            Response.Headers.Add("Surrogate-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
            Response.Headers.Add("Expires", "0");

            try
            {
                // If a user already requested the image within 30 seconds, use that instead.
                if ((DateTime.Now - _bannerCacheService.CacheUpdateTime).TotalSeconds <= 30)
                {
                    return File(Encoding.ASCII.GetBytes(_bannerCacheService.OutputCache), "image/svg+xml");
                }

                var svgFile = await System.IO.File.ReadAllTextAsync("banner.svg");
                var bannerDownloadList = new List<Task<BeatmapCoverData>>();

                for (int i = 0; i < _bot.Lobbies.Count; i++)
                {
                    var lobby = _bot.Lobbies[i];

                    // Apply lobby name and player count
                    
                    svgFile = svgFile.Replace($"$NAME{i}", HttpUtility.HtmlEncode(lobby.Configuration.Name));
                    svgFile = svgFile.Replace($"$PLAYERS{i}", $"{lobby.MultiplayerLobby.Players.Count:00}/{lobby.Configuration.Size}");

                    var behaviour = (MapManagerBehaviour?)lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
                    if (behaviour == null)
                    {
                        continue;
                    }

                    // Clamp and apply map name
                    var mapName = behaviour.CurrentBeatmapName;

                    if (mapName.Length > 40)
                        mapName = mapName[..40] + "...";

                    svgFile = svgFile.Replace($"$MAP{i}", $"{HttpUtility.HtmlEncode(mapName)}");

                    if (behaviour.CurrentBeatmapSetId == 0)
                    {
                        continue;
                    }

                    // If we've already downloaded the banner image, re-use that instead.
                    if (_bannerCacheService.BeatmapCoverCache[i]?.Id == behaviour.CurrentBeatmapSetId)
                    {
                        svgFile = svgFile.Replace($"$BANNER{i}", $"data:image/png;base64, {_bannerCacheService.BeatmapCoverCache[i]?.Data}");

                        continue;
                    }

                    int lobbyIndex = i; // A copy of "i" is intentional here, as it needs to be used asynchronously

                    bannerDownloadList.Add(
                        Task.Run(async () =>
                        {
                            var bannerImageResponse = await _httpClient.GetAsync(
                                $"https://assets.ppy.sh/beatmaps/{behaviour.CurrentBeatmapSetId}/covers/cover.jpg");

                            var ret = new BeatmapCoverData()
                            {
                                Id = behaviour.CurrentBeatmapSetId,
                                LobbyIndex = lobbyIndex
                            };

                            if (bannerImageResponse.IsSuccessStatusCode)
                            {
                                ret.Data = Convert.ToBase64String(await bannerImageResponse.Content.ReadAsByteArrayAsync());
                            }

                            return ret;
                        })
                    );
                }

                // Wait for any existing banner image downloads to complete
                var bannerImageDownloads = await Task.WhenAll(bannerDownloadList);

                foreach (var imgResult in bannerImageDownloads)
                {
                    // Save the image for the future
                    _bannerCacheService.BeatmapCoverCache[imgResult.LobbyIndex] = imgResult;

                    svgFile = svgFile.Replace($"$BANNER{imgResult.LobbyIndex}", $"data:image/png;base64, {imgResult.Data}");
                }

                _bannerCacheService.CacheUpdateTime = DateTime.Now;
                _bannerCacheService.OutputCache = svgFile;

                return File(Encoding.ASCII.GetBytes(_bannerCacheService.OutputCache), "image/svg+xml");
            }
            catch (Exception e)
            {
                Serilog.Log.Error($"Error while generating image banner: {e.ToString()}");

                return BadRequest();
            }
        }

        [HttpGet("join/{lobbyId}")]
        public async Task<ActionResult> GetJoinLink(int lobbyId)
        {
            // I currently have no clue how to get these programatically, so I'll hard code them for now.
            // Possibly look into doing "!mp invite", then grabbing the id from the invite link. If all of that fails
            // then I'll rewrite this to read it from a file/the config.

            switch (lobbyId)
            {
                case 0:
                    return Redirect("osu://mp/19454");
                case 1:
                    return Redirect("osu://mp/19455");
                case 2:
                    return Redirect("osu://mp/19456");
                case 3:
                    return Redirect("osu://mp/14500");
                default:
                    break;
            }

            return BadRequest();
        }
    }
}
