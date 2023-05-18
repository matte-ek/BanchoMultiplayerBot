using System.Net;
using System.Text.Json;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using Serilog;

namespace BanchoMultiplayerBot.OsuApi;

/// <summary>
/// Utility wrapper for the osu!api v1, requires an API key to be setup.
/// </summary>
public class OsuApiWrapper
{
    private static readonly HttpClient Client = new();
    private readonly string _osuApiKey;

    public OsuApiWrapper(string osuApiKey)
    {
        _osuApiKey = osuApiKey;
    }

    public async Task<BeatmapModel?> GetBeatmapInformation(int beatmapId, int mods = 0)
    {
        var result = await Client.GetAsync($"https://osu.ppy.sh/api/get_beatmaps?k={_osuApiKey}&b={beatmapId}&mods={mods}");
        
        if (!result.IsSuccessStatusCode)
        {
            Log.Error($"Error code {result.StatusCode} while getting beatmap details for id {beatmapId}!");

            return result.StatusCode switch
            {
                HttpStatusCode.Unauthorized => throw new ApiKeyInvalidException(),
                _ => null
            };
        }

        string jsonStr = await result.Content.ReadAsStringAsync();

        try
        {
            var maps = JsonSerializer.Deserialize<List<BeatmapModel>>(jsonStr);

            if (maps != null && !maps.Any())
                throw new BeatmapNotFoundException(); // the API does not return 404 for some reason, just an empty list.
            
            return maps?.FirstOrDefault();
        }
        catch (Exception e)
        {
            Log.Error($"Error while parsing JSON from osu!api: {e.Message}, beatmap: {beatmapId}");

            throw;
        }
    }
}