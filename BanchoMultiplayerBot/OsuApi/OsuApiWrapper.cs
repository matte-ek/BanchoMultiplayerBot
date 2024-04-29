using System.Net;
using System.Text.Json;
using BanchoMultiplayerBot.OsuApi.Exceptions;
//using Prometheus;
using Serilog;

namespace BanchoMultiplayerBot.OsuApi;

/// <summary>
/// Utility wrapper for the osu!api v1, requires an API key to be setup.
/// </summary>
public class OsuApiWrapper
{
    private readonly string _osuApiKey;
    private readonly Bot _bot;

    public OsuApiWrapper(Bot bot, string osuApiKey)
    {
        _bot = bot;
        _osuApiKey = osuApiKey;
    }

    public async Task<BeatmapModel?> GetBeatmapInformation(int beatmapId, int mods = 0)
    {
        using var httpClient = new HttpClient();
        //using var _ = _bot.RuntimeInfo.Statistics.ApiRequestTime.NewTimer();

        //_bot.RuntimeInfo.Statistics.ApiRequests.Inc();

        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_beatmaps?k={_osuApiKey}&b={beatmapId}&mods={mods}");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error($"Error code {result.StatusCode} while getting beatmap details for id {beatmapId}!");

                //_bot.RuntimeInfo.Statistics.ApiErrors.Inc();

                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new ApiKeyInvalidException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var maps = JsonSerializer.Deserialize<List<BeatmapModel>>(json);

            if (maps != null && !maps.Any())
                throw new BeatmapNotFoundException(); // the API returns 200 even if it got no results.

            return maps?.FirstOrDefault();
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Exception during osu!api request: {e.Message}, beatmap: {beatmapId}");

            // _bot.RuntimeInfo.Statistics.ApiErrors.Inc();

            throw;
        }
    }
    
    public async Task<ScoreModel?> GetRecentScore(string player)
    {
        using var httpClient = new HttpClient();
        //using var _ = _bot.RuntimeInfo.Statistics.ApiRequestTime.NewTimer();

        //_bot.RuntimeInfo.Statistics.ApiRequests.Inc();

        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_user_recent?k={_osuApiKey}&u={player}&limit=1");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error($"Error code {result.StatusCode} while getting recent score for player {player}!");

                //_bot.RuntimeInfo.Statistics.ApiErrors.Inc();

                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new ApiKeyInvalidException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var scores = JsonSerializer.Deserialize<List<ScoreModel>>(json);

            if (scores != null && !scores.Any())
                throw new BeatmapNotFoundException(); // the API returns 200 even if it got no results.

            return scores?.FirstOrDefault();
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Exception during osu!api request: {e.Message}, player: {player}");

            // _bot.RuntimeInfo.Statistics.ApiErrors.Inc();

            throw;
        }
    }
    
    public async Task<IReadOnlyList<LeaderboardScoreModel>?> GetLeaderboardScores(int beatmapId)
    {
        using var httpClient = new HttpClient();
        // using var _ = _bot.RuntimeInfo.Statistics.ApiRequestTime.NewTimer();

        //_bot.RuntimeInfo.Statistics.ApiRequests.Inc();

        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_scores?k={_osuApiKey}&b={beatmapId}");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error($"Error code {result.StatusCode} while getting leaderboard from map {beatmapId}!");

                // _bot.RuntimeInfo.Statistics.ApiErrors.Inc();

                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new ApiKeyInvalidException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var scores = JsonSerializer.Deserialize<List<LeaderboardScoreModel>>(json);

            if (scores != null && !scores.Any())
                throw new BeatmapNotFoundException(); // the API returns 200 even if it got no results.

            return scores;
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error($"Exception during osu!api request: {e.Message}, beatmap: {beatmapId}");

            // _bot.RuntimeInfo.Statistics.ApiErrors.Inc();

            throw;
        }
    }
    
    public async Task<List<ScoreModel?>> GetRecentScoresBatch(List<string?> players)
    {
        try
        {
            var apiRequests = players.Select(player => Task.Run(() => GetRecentScore(player))).ToList();

            Log.Information($"Running osu!api batch request for {apiRequests.Count} players");
            
            await Task.WhenAll(apiRequests);

            return apiRequests.Select(x => x.Result).ToList();
        }
        catch (Exception e)
        {
            Log.Error($"Exception during batch osu!api request: {e.Message}");
            
            throw;
        }

        return new List<ScoreModel?>();
    }
}