using System.Net;
using System.Text.Json;
using BanchoMultiplayerBot.Osu.Exceptions;
using BanchoMultiplayerBot.Osu.Models;
using Serilog;

namespace BanchoMultiplayerBot.Osu;

public class OsuApi(string apiKey)
{
    public async Task<BeatmapModel?> GetBeatmapInformation(int beatmapId, ModsModel mods = 0)
    {
        using var httpClient = new HttpClient();
        
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            Log.Verbose("OsuApi: Requesting beatmap information for map {BeatmapId}...", beatmapId);
            
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_beatmaps?k={apiKey}&b={beatmapId}&mods={(int)mods}");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error("OsuApi: Failed request to get beatmap information for map {BeatmapId}, status code: {StatusCode}", beatmapId, result.StatusCode);
                
                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new InvalidApiKeyException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var maps = JsonSerializer.Deserialize<List<BeatmapModel>>(json);

            if (maps != null && !maps.Any())
            {
                throw new BeatmapNotFoundException(); // The API returns 200 even if it got no results.
            }
            
            return maps?.FirstOrDefault();
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error(e, "OsuApi: Exception while getting beatmap information for map {BeatmapId}, {e}", beatmapId, e);
            throw;
        }
    }
    
    public async Task<ScoreModel?> GetRecentScore(string playerName)
    {
        using var httpClient = new HttpClient();
        
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            Log.Verbose("OsuApi: Requesting recent score for player {PlayerName}...", playerName);
            
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_user_recent?k={apiKey}&u={playerName}&limit=1");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error("OsuApi: Failed request to get recent score for player {PlayerName}, status code: {StatusCode}", playerName, result.StatusCode);
                
                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new InvalidApiKeyException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var scores = JsonSerializer.Deserialize<List<ScoreModel>>(json);

            if (scores != null && !scores.Any())
            {
                throw new NoScoreFoundException(); // the API returns 200 even if it got no results.
            }
                
            return scores?.FirstOrDefault();
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error(e, "OsuApi: Exception while getting recent score for player {PlayerName}, {e}", playerName, e);
            throw;
        }
    }    
    
    public async Task<IReadOnlyList<ScoreModel?>> GetRecentScores(IEnumerable<string> players)
    {
        try
        {
            var apiRequests = players.Select(player => Task.Run(() => GetRecentScore(player))).ToList();
            
            Log.Verbose("OsuApi: Requesting {Count} recent scores...", apiRequests.Count);
            
            await Task.WhenAll(apiRequests);

            return apiRequests.Select(x => x.Result).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "OsuApi: Exception while getting recent scores for players, {e}", e);
            
            throw;
        }
    }
    
    public async Task<IReadOnlyList<LeaderboardScoreModel>?> GetMapLeaderboardScores(int beatmapId)
    {
        using var httpClient = new HttpClient();
        
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            Log.Verbose("OsuApi: Requesting leaderboard scores for map {BeatmapId}...", beatmapId);
            
            var result = await httpClient.GetAsync($"https://osu.ppy.sh/api/get_scores?k={apiKey}&b={beatmapId}");

            if (!result.IsSuccessStatusCode)
            {
                Log.Error("OsuApi: Failed request to get leaderboard scores for map {BeatmapId}, status code: {StatusCode}", beatmapId, result.StatusCode);
                
                return result.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => throw new InvalidApiKeyException(),
                    _ => null
                };
            }

            var json = await result.Content.ReadAsStringAsync();
            var scores = JsonSerializer.Deserialize<List<LeaderboardScoreModel>>(json);

            if (scores != null && !scores.Any())
            {
                throw new NoScoreFoundException(); // the API returns 200 even if it got no results.
            }
                
            return scores;
        }
        catch (BeatmapNotFoundException)
        {
            throw;
        }
        catch (Exception e)
        {
            Log.Error(e, "OsuApi: Exception while getting leaderboard scores for map {BeatmapId}, {e}", beatmapId, e);
            
            throw;
        }
    }
}