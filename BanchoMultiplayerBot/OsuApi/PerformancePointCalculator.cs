using System.Diagnostics;
using BanchoMultiplayerBot.Data;
using Serilog;

namespace BanchoMultiplayerBot.OsuApi;

/// <summary>
/// Utility class to interface with osu-tools and calculate performance points of beatmaps.
/// This is not rather efficient since it will literally run osu-tools processes on your system each time.
/// However the alternatives weren't too appealing anyway, and this will work just fine for this purpose. 
/// </summary>
public class PerformancePointCalculator
{
    public const string OsuToolsDirectory = "../osu-pp-tools";
    public static bool IsAvailable => Directory.Exists(OsuToolsDirectory);
    
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Calculates the pp values for 100%, 98% and 95% with NM for the specified beatmap.
    /// </summary>
    public async Task<BeatmapPerformanceInfo?> CalculatePerformancePoints(int beatmapId)
    {
        const string cacheDir = $"{OsuToolsDirectory}/cache";
        var beatmapFilePath = $"{cacheDir}/{beatmapId}.osu";
        
        // This should never be the case but better safe than sorry.
        if (!Directory.Exists(OsuToolsDirectory))
        {
            Log.Error("Failed to find osu tools directory.");
            return null;
        }
        
        if (!Directory.Exists(cacheDir))
            Directory.CreateDirectory(cacheDir);
     
        // Download the beatmap (if necessary), this will only download the beatmap itself (.osu), without any additional media.
        if (!File.Exists(beatmapFilePath))
        {
            try
            {
                var downloadTask = _httpClient.GetStreamAsync(new Uri($"https://osu.ppy.sh/osu/{beatmapId}"));

                await using var s = await downloadTask.WaitAsync(TimeSpan.FromSeconds(5));
                await using var fs = new FileStream(beatmapFilePath, FileMode.CreateNew);
                
                await s.CopyToAsync(fs);
            }
            catch (Exception e)
            {
                Log.Error($"Error while downloading beatmap {beatmapId} ({e.Message})");
            }
        }

        try
        {
            var pp100 = CalculateBeatmapPerformancePoints(beatmapId, 100);
            var pp98 = CalculateBeatmapPerformancePoints(beatmapId, 98);
            var pp95 = CalculateBeatmapPerformancePoints(beatmapId, 95);
            
            var ppResults100 = await pp100;
            var ppResults98 = await pp98;
            var ppResults95 = await pp95;

            if (ppResults100 == null ||
                ppResults98 == null ||
                ppResults95 == null)
            {
                // If any of these are null, we should have logged a reason earlier (hopefully).
                return null;
            }
            
            return new BeatmapPerformanceInfo
            {
                Performance100 = ppResults100.Value,
                Performance98 = ppResults98.Value,
                Performance95 = ppResults95.Value,
            };;
        }
        catch (Exception e)
        {
            Log.Error($"Error while calculating pp for map for beatmap {beatmapId} ({e.Message})");
            return null;
        }
    }

    /// <summary>
    /// Calculates the pp for beatmap with the specified accuracy, has a 5 second timeout.  
    /// </summary>
    private async Task<int?> CalculateBeatmapPerformancePoints(int beatmapId, int acc)
    {
        // osu-tools will be ran with "-j", which will cause it to output the data in JSON format.
        var performanceCalcProcess = RunProcessAsync($"dotnet", $"../osu-pp-tools/PerformanceCalculator.dll simulate osu -a {acc} -j ../osu-pp-tools/cache/{beatmapId}.osu");

        try
        {
            string output = await performanceCalcProcess.WaitAsync(TimeSpan.FromSeconds(5));

            // Quick check to make sure we're actually throwing in JSON into DeserializeObject later on.
            if (!(output.StartsWith("{") && output.EndsWith("}")))
            {
                Log.Error($"Error while calculating pp for map, PerformanceCalculator probably threw an exception. Id: {beatmapId}");
                return null;
            }

            // The code below is prone to failure, but it's fine for this use case.
            dynamic performanceData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(output)!;

            double pp = performanceData.performance_attributes.pp;

            return Convert.ToInt32(Math.Round(pp));
        }
        catch (TimeoutException)
        {
            Log.Error($"Error while calculating pp for beatmap {beatmapId}, reached 5 second timeout.");
        }
        catch (Exception e)
        {
            Log.Error($"Error while calculating pp for beatmap {beatmapId}, {e.Message}");
        }

        return null;
    }
    
    /// <summary>
    /// Returns a task to run a process asynchronously, end result is STDOUT.
    /// This should be use with caution, make sure no weird user input is sent.
    /// </summary>
    private Task<string> RunProcessAsync(string cmd, string arguments)
    {
        var taskCompletionSource = new TaskCompletionSource<string>();
        
        var process = new Process
        {
            StartInfo =
            {
                FileName = cmd,
                Arguments = arguments,
                RedirectStandardOutput = true,
            },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) =>
        {
            string stdOut = process.StandardOutput.ReadToEnd();
            
            taskCompletionSource.SetResult(stdOut);
            
            process.Dispose();
        };

        process.Start();

        return taskCompletionSource.Task;
    }
}