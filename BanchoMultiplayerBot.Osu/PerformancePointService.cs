using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BanchoMultiplayerBot.Osu.Data;
using BanchoMultiplayerBot.Osu.Extensions;
using osu.NET.Models.Beatmaps;
using osu.NET.Models.Scores;
using Serilog;

namespace BanchoMultiplayerBot.Osu;

public class PerformancePointService(string? performancePointServiceUrl, string? beatmapCacheDirectory)
{
    public bool IsAvailable => performancePointServiceUrl != null;

    private readonly HttpClient _httpClient = new();

    private readonly string _cacheDirectoryPath = beatmapCacheDirectory ?? "cache";

    /// <summary>
    /// Calculates the pp values for 100%, 98% and 95% with NM for the specified beatmap.
    /// </summary>
    public async Task<BeatmapPerformanceInfo?> CalculatePerformancePoints(int beatmapId, int mods = 0, DateTimeOffset? lastUpdatedHint = null)
    {
        if (!await DownloadBeatmapFile(beatmapId, lastUpdatedHint))
        {
            return null;
        }

        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"{performancePointServiceUrl}/pp/map")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    map_id = beatmapId,
                    mods
                }), 
                Encoding.UTF8,
                "application/json")
            };
            
            var response = await _httpClient.SendAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("{Component}: Request to performance point service for map data failed, status code: {StatusCode}", nameof(PerformancePointService), response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<BeatmapPerformanceInfo>();
            if (data == null)
            {
                Log.Error("{Component}: Parsing response from performance point for map data service failed", nameof(PerformancePointService));
                return null;
            }
            
            return data;
        }
        catch (Exception e)
        {
            Log.Error("PerformancePointCalculator: Error while calculating pp for beatmap {BeatmapId}, {e.Message}", beatmapId, e);
            return null;
        }
    }

    /// <summary>
    /// Calculates the pp for an individual score.
    /// </summary>
    public async Task<ScorePerformanceInfo?> CalculateScorePerformancePoints(int beatmapId, Score scoreModel, DateTimeOffset? lastUpdated = null)
    {
        if (!await DownloadBeatmapFile(beatmapId, lastUpdated))
        {
            return null;
        }

        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, $"{performancePointServiceUrl}/pp/score")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        map_id = beatmapId,
                        n300 = scoreModel.Statistics.Great,
                        n100 = scoreModel.Statistics.Ok,
                        n50 = scoreModel.Statistics.Meh,
                        miss = scoreModel.Statistics.Miss,
                        max_combo = scoreModel.MaxCombo,
                        mods = scoreModel.GetModsBitset()
                    }), 
                    Encoding.UTF8,
                    "application/json")
            };
            
            var response = await _httpClient.SendAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("{Component}: Request to performance point service for score data failed, status code: {StatusCode}", nameof(PerformancePointService), response.StatusCode);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<ScorePerformanceInfo>();
            if (data == null)
            {
                Log.Error("{Component}: Parsing response from performance point service for score data failed", nameof(PerformancePointService));
                return null;
            }
            
            return data;
        }
        catch (Exception e)
        {
            Log.Error("PerformancePointCalculator: Error while calculating pp for score {ScoreId}, {e.Message}", scoreModel.Id, e);
            return null;
        }
    }

    private async Task<bool> DownloadBeatmapFile(int beatmapId, DateTimeOffset? lastUpdatedHint = null)
    {
        var beatmapFilePath = $"{_cacheDirectoryPath}/{beatmapId}.osu";

        // This should never be the case but better safe than sorry.
        if (!IsAvailable)
        {
            Log.Error("PerformancePointCalculator: performance-calculator is not available.");
            return false;
        }
        
        if (!Directory.Exists(_cacheDirectoryPath))
        {
            Directory.CreateDirectory(_cacheDirectoryPath);
        }

        if (File.Exists(beatmapFilePath))
        {
            var fileWrittenTime = File.GetLastWriteTimeUtc(beatmapFilePath);
            
            if (lastUpdatedHint != null &&
                lastUpdatedHint.Value.UtcDateTime > fileWrittenTime)
            {
                Log.Warning("PerformancePointCalculator: Beatmap id {BeatmapId} was found to be outdated ({LastUpdatedTime} > {LastDownloadedTime}), downloading update...",
                    beatmapId, 
                    lastUpdatedHint.Value.UtcDateTime,
                    fileWrittenTime);
                
                File.Delete(beatmapFilePath);
            }
            else
            {
                return true;
            }
        }
        
        // Download the beatmap, this will only download the beatmap itself (.osu), without any additional media.
        
        try
        {
            var downloadTask = _httpClient.GetStreamAsync(new Uri($"https://osu.ppy.sh/osu/{beatmapId}"));

            await using var s = await downloadTask.WaitAsync(TimeSpan.FromSeconds(5));
            await using var fs = new FileStream(beatmapFilePath, FileMode.CreateNew);

            await s.CopyToAsync(fs);
        }
        catch (Exception e)
        {
            Log.Error("{Component}: Failed to download beatmap {BeatmapId}, {e.Message}", nameof(PerformancePointService), beatmapId, e);
            return false;
        }

        return true;
    }
}