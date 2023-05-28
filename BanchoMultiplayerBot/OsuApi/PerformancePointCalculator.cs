using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using BanchoMultiplayerBot.Data;
using Serilog;

namespace BanchoMultiplayerBot.OsuApi;

/// <summary>
/// Utility class to interface with performance calculator and calculate performance points of beat maps.
/// </summary>
public class PerformancePointCalculator
{
    public static bool IsAvailable => File.Exists("performance-calculator.exe") || File.Exists("performance-calculator");
    
    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Calculates the pp values for 100%, 98% and 95% with NM for the specified beatmap.
    /// </summary>
    public async Task<BeatmapPerformanceInfo?> CalculatePerformancePoints(int beatmapId)
    {
        const string cacheDir = $"cache";
        var beatmapFilePath = $"{cacheDir}/{beatmapId}.osu";
        
        // This should never be the case but better safe than sorry.
        if (!IsAvailable)
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
            return await CalculateBeatmapPerformancePoints(beatmapId);
        }
        catch (Exception e)
        {
            Log.Error($"Error while calculating pp for map for beatmap {beatmapId} ({e.Message})");
            return null;
        }
    }

    /// <summary>
    /// Calculates the pp for beatmap, has a 5 second timeout.  
    /// </summary>
    private async Task<BeatmapPerformanceInfo?> CalculateBeatmapPerformancePoints(int beatmapId)
    {
        var appName = "performance-calculator" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);
        var performanceCalcProcess = RunProcessAsync(appName, $"{beatmapId}");

        if (performanceCalcProcess == null)
        {
            return null;
        }
        
        try
        {
            string output = await performanceCalcProcess.WaitAsync(TimeSpan.FromSeconds(5));

            if (output.StartsWith("err: "))
            {
                Log.Error($"Failed to calculate pp for beatmap {beatmapId}: {output}");

                return null;
            }

            var values = output.Split('\n');
            if (values.Length < 4)
            {
                Log.Error($"Failed to calculate pp for beatmap {beatmapId}: {output}");
                return null;
            }

            return new BeatmapPerformanceInfo()
            {
                Performance100 = (int)Math.Round(Convert.ToDouble(values[0], CultureInfo.InvariantCulture)),
                Performance98 = (int)Math.Round(Convert.ToDouble(values[1], CultureInfo.InvariantCulture)),
                Performance95 = (int)Math.Round(Convert.ToDouble(values[2], CultureInfo.InvariantCulture)),
            };
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
    private Task<string>? RunProcessAsync(string cmd, string arguments)
    {
        try
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
                try
                {
                    string stdOut = process.StandardOutput.ReadToEnd();
            
                    taskCompletionSource.SetResult(stdOut);
            
                    process.Dispose();
                }
                catch (Exception e)
                {
                    // ignored
                }
            };

            process.Start();

            return taskCompletionSource.Task;
        }
        catch (Exception e)
        {
            Log.Error($"Exception at RunProcessAsync: {e}");
            return null;
        }
    }
}