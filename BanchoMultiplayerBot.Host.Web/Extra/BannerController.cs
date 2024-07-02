﻿using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Host.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Serilog;
using Microsoft.FeatureManagement.Mvc;

namespace BanchoMultiplayerBot.Host.Web.Extra
{
    /// <summary>
    /// This is a gimmick to show the lobbies status as an embeddable image.
    /// It's more or less currently hardcoded to show less or equal to 4 lobbies, depends on the input SVG
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    [FeatureGate("BannerEndpoint")]
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
                // Make sure we have the placeholder image present if required
                if (!_bannerCacheService.PlaceholderImage.Any() && System.IO.File.Exists("placeholder.png"))
                {
                    _bannerCacheService.PlaceholderImage = Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync("placeholder.png"));
                }

                // If a user already requested the image within 30 seconds, use that instead.
                if ((DateTime.Now - _bannerCacheService.CacheUpdateTime).TotalSeconds <= 30)
                {
                    return File(Encoding.ASCII.GetBytes(_bannerCacheService.OutputCache), "image/svg+xml");
                }

                var svgFile = await System.IO.File.ReadAllTextAsync("banner.svg");
                var bannerDownloadList = new List<Task<BeatmapCoverData>>();

                for (var i = 0; i < _bot.Lobbies.Count; i++)
                {
                    var lobby = _bot.Lobbies[i];

                    // Apply lobby name and player count
                    
                    svgFile = svgFile.Replace($"$NAME{i}", HttpUtility.HtmlEncode(lobby.Configuration.Name));
                    svgFile = svgFile.Replace($"$PLAYERS{i}", $"{lobby.MultiplayerLobby.Players.Count:00}/{lobby.Configuration.Size}");

                    var behaviour = (MapManagerBehaviour?)lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
                    if (behaviour?.CurrentBeatmap == null)
                    {
                        continue;
                    }

                    // Clamp and apply map name
                    var mapName = behaviour.CurrentBeatmap.Name;

                    if (mapName.Length > 35)
                        mapName = mapName[..35] + "...";

                    var starRatingColor = GetOsuDifficultyColor(behaviour.CurrentBeatmap.StarRating);
                    var textColor = ((starRatingColor.Red * 255) * 0.299 + (starRatingColor.Green * 255) * 0.587 + (starRatingColor.Blue * 255) * 0.114) > 186
                        ? new Color(35, 35, 35, 255)
                        : new Color(240, 240, 240, 255);
                    
                    svgFile = svgFile.Replace($"$MAP{i}", $"{HttpUtility.HtmlEncode(mapName)}");
                    svgFile = svgFile.Replace($"$SR{i}", $"{behaviour.CurrentBeatmap.StarRating:.00}");
                    
                    svgFile = svgFile.Replace($"$SR_CLR{i}", starRatingColor.ToCssString());
                    svgFile = svgFile.Replace($"$SR_TXT{i}", textColor.ToCssString());

                    if (behaviour.CurrentBeatmap.SetId == 0)
                    {
                        continue;
                    }

                    // If we've already downloaded the banner image, re-use that instead.
                    if (_bannerCacheService.BeatmapCoverCache[i]?.Id == behaviour.CurrentBeatmap.SetId)
                    {
                        svgFile = svgFile.Replace($"$BANNER{i}", $"data:image/png;base64, {_bannerCacheService.BeatmapCoverCache[i]?.Data}");

                        continue;
                    }

                    int lobbyIndex = i; // A copy of "i" is intentional here, as it needs to be used asynchronously

                    bannerDownloadList.Add(
                        Task.Run(async () =>
                        {
                            var bannerImageResponse = await _httpClient.GetAsync(
                                $"https://assets.ppy.sh/beatmaps/{behaviour.CurrentBeatmap.SetId}/covers/cover.jpg");

                            var ret = new BeatmapCoverData
                            {
                                Id = behaviour.CurrentBeatmap.SetId,
                                LobbyIndex = lobbyIndex,
                                Data = bannerImageResponse.IsSuccessStatusCode ? Convert.ToBase64String(await bannerImageResponse.Content.ReadAsByteArrayAsync()) : _bannerCacheService.PlaceholderImage
                            };

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
        public ActionResult GetJoinLink(int lobbyId)
        {
            if (0 > lobbyId || lobbyId >= _bot.Lobbies.Count)
            {
                return BadRequest();
            }

            var lobby = _bot.Lobbies[lobbyId];

            return Redirect( $"osu://mp/{lobby.LobbyJoinId}");
        }

        // See https://github.com/ppy/osu/blob/master/osu.Game/Graphics/OsuColour.cs#L26
        private static Color GetOsuDifficultyColor(float starRating)
        {
            return Color.SampleFromLinearGradient(new[]
            {
                (0.1f, new Color(170, 170, 170, 255)),
                    (0.1f, new Color(66, 144, 251, 255)),
                (1.25f, new Color(79, 192, 255, 255)),
                (2.0f, new Color(79, 255, 213, 255)),
                (2.5f, new Color(124, 255, 79, 255)),
                (3.3f, new Color(246, 240, 92, 255)),
                (4.2f, new Color(255, 128, 104, 255)),
                (4.9f, new Color(255, 78, 111, 255)),
                (5.8f, new Color(198, 69, 184, 255)),
                (6.7f, new Color(101, 99, 222, 255)),
                (7.7f, new Color(24, 21, 142, 255)),
                (9.0f, new Color(0, 0, 0, 255)),
            }, (float)Math.Round(starRating, 2, MidpointRounding.AwayFromZero));
        }
        
    }
}
