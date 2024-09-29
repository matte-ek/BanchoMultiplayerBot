using System.Globalization;
using System.Text;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Host.WebApi.Data;
using BanchoMultiplayerBot.Providers;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class BannerService(BannerCacheService bannerCacheService, Bot bot)
{
    public async Task<string?> GetBanner()
    {
        // If a user already requested the image within 30 seconds, use that instead.
        if ((DateTime.UtcNow - bannerCacheService.CacheUpdateTime).TotalSeconds <= 1)
        {
            return bannerCacheService.OutputCache;
        }
        
        var finalSvg = await File.ReadAllTextAsync("banner/banner.svg");
        var lobbyCssTemplate = await File.ReadAllTextAsync("banner/lobby-css-template.css");
        var lobbySvgTemplate = await File.ReadAllTextAsync("banner/lobby-svg-template.svg");
        var generateLobbyTasks = new List<Task<LobbySvgData>>();
        
        foreach (var (lobbyIt, indexIt) in bot.Lobbies.Select((lobby, index) => (lobby, index)))
        {
            var lobby = lobbyIt;
            var index = indexIt;
            
            generateLobbyTasks.Add(Task.Run(async () =>
            {
                var lobbyConfig = await lobby.GetLobbyConfiguration();
                var mapManagerDataProvider = new BehaviorDataProvider<MapManagerBehaviorData>(lobby);
                
                var starRatingColor = GetOsuDifficultyColor(mapManagerDataProvider.Data.BeatmapInfo.StarRating);
                var starRatingTextColor = ((starRatingColor.Red * 255) * 0.299 + (starRatingColor.Green * 255) * 0.587 + (starRatingColor.Blue * 255) * 0.114) > 186
                    ? new Color(35, 35, 35, 255)
                    : new Color(240, 240, 240, 255);
                
                var mapLength = mapManagerDataProvider.Data.BeatmapInfo.Length;
                var mapProgress = DateTime.UtcNow - mapManagerDataProvider.Data.MatchStartTime;
                var mapProgressPercentage = (int)((mapProgress.TotalSeconds / mapLength.TotalSeconds) * 100f);
                
                // Prepare SVG
                var lobbySvg = lobbySvgTemplate.Replace("$ID$", index.ToString());
                
                lobbySvg = lobbySvg.Replace("$NAME$", lobbyConfig.Name);
                lobbySvg = lobbySvg.Replace("$MAP$", $"{mapManagerDataProvider.Data.BeatmapInfo.Artist} - {mapManagerDataProvider.Data.BeatmapInfo.Name}");
                lobbySvg = lobbySvg.Replace("$SR$", mapManagerDataProvider.Data.BeatmapInfo.StarRating.ToString("0.00"));
                lobbySvg = lobbySvg.Replace("$Y$", (index * 220).ToString());
                
                // Prepare CSS
                var lobbyCss = lobbyCssTemplate.Replace("$ID$", index.ToString());
                
                lobbyCss = lobbyCss.Replace("$SR-TEXT-COLOR$", starRatingTextColor.ToCssString());
                lobbyCss = lobbyCss.Replace("$SR-BG-COLOR$", starRatingColor.ToCssString());
             
                lobbyCss = lobbyCss.Replace("$LEN-REMAINING-SEC$", ((mapLength - mapProgress).TotalSeconds > 0 ? (mapLength - mapProgress).TotalSeconds : 0).ToString("0"));
                lobbyCss = lobbyCss.Replace("$LEN-PER$", (mapProgressPercentage > 100 ? 0 : mapProgressPercentage).ToString(CultureInfo.InvariantCulture));
                
                // Grab the banner image, if necessary.
                if (bannerCacheService.BeatmapCoverCache[index]?.Id != mapManagerDataProvider.Data.BeatmapInfo.SetId)
                {
                    using var httpClient = new HttpClient();

                    var bannerImageResponse = await httpClient.GetAsync(
                        $"https://assets.ppy.sh/beatmaps/{mapManagerDataProvider.Data.BeatmapInfo.SetId}/covers/cover.jpg");
                    
                    var data = new BeatmapCoverData
                    {
                        Id = mapManagerDataProvider.Data.BeatmapInfo.SetId,
                        LobbyIndex = index,
                        Data = bannerImageResponse.IsSuccessStatusCode ? Convert.ToBase64String(await bannerImageResponse.Content.ReadAsByteArrayAsync()) : "0"
                    };
                    
                    bannerCacheService.BeatmapCoverCache[index] = data;
                }
        
                lobbyCss = lobbyCss.Replace("$IMAGE$", $"data:image/png;base64, {bannerCacheService.BeatmapCoverCache[index]?.Data}");

                return new LobbySvgData
                {
                    Svg = lobbySvg,
                    Css = lobbyCss
                };
            }));
        }

        await Task.WhenAll(generateLobbyTasks);
        
        var lobbyData = generateLobbyTasks.Select(x => x.Result).ToList();
        
        finalSvg = finalSvg.Replace("$LOBBY_CSS_TEMPLATE$", string.Join("\n", lobbyData.Select(x => x.Css)));
        finalSvg = finalSvg.Replace("$LOBBY_SVG_TEMPLATE$", string.Join("\n", lobbyData.Select(x => x.Svg)));
        
        bannerCacheService.OutputCache = finalSvg;
        bannerCacheService.CacheUpdateTime = DateTime.UtcNow;
        
        return finalSvg;
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

    private class LobbySvgData
    {
        public string Svg { get; set; } = string.Empty;
        public string Css { get; set; } = string.Empty;
    }
}