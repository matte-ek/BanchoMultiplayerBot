using System.Net;
using System.Text.Json;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using BanchoSharp;
using Serilog;

namespace BanchoMultiplayerBot.OsuApi;

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
                HttpStatusCode.NotFound => throw new BeatmapNotFoundException(),
                _ => null
            };
        }

        string jsonStr = await result.Content.ReadAsStringAsync();

        try
        {
            return JsonSerializer.Deserialize<List<BeatmapModel>>(jsonStr)?.FirstOrDefault();
        }
        catch (Exception e)
        {
            Log.Error($"Error while parsing json from osu!api: {e.Message}, beatmap: {beatmapId}");

            throw;
        }
    }
}