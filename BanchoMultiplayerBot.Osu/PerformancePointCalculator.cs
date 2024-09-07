using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using BanchoMultiplayerBot.Osu.Data;
using BanchoMultiplayerBot.Osu.Extensions;
using BanchoMultiplayerBot.Osu.Interfaces;
using OsuSharp.Models.Scores;
using Serilog;

namespace BanchoMultiplayerBot.Osu;

public class PerformancePointCalculator
{
    public static bool IsAvailable => File.Exists("performance-calculator.exe") || File.Exists("performance-calculator");

    private readonly HttpClient _httpClient = new();

    /// <summary>
    /// Calculates the pp values for 100%, 98% and 95% with NM for the specified beatmap.
    /// </summary>
    public async Task<BeatmapPerformanceInfo?> CalculatePerformancePoints(int beatmapId, int mods = 0)
    {
        if (!await DownloadBeatmapFile(beatmapId))
        {
            return null;
        }

        try
        {
            return (BeatmapPerformanceInfo?)await CalculateBeatmapPerformancePoints(beatmapId, mods);
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
    public async Task<PlayPerformanceInfo?> CalculateScorePerformancePoints(int beatmapId, Score scoreModel)
    {
        if (!await DownloadBeatmapFile(beatmapId))
        {
            return null;
        }

        try
        {
            return (PlayPerformanceInfo?)await CalculateBeatmapPerformancePoints(beatmapId,
                scoreModel.GetModsBitset(),
                (scoreModel.Statistics.Count300),
                (scoreModel.Statistics.Count100),
                (scoreModel.Statistics.Count50),
                (scoreModel.Statistics.Misses),
                (scoreModel.MaxCombo));
        }
        catch (Exception e)
        {
            Log.Error("PerformancePointCalculator: Error while calculating pp for score {ScoreId}, {e.Message}", scoreModel.Id, e);
            return null;
        }
    }

    private async Task<bool> DownloadBeatmapFile(int beatmapId)
    {
        const string cacheDir = $"cache";
        var beatmapFilePath = $"{cacheDir}/{beatmapId}.osu";

        // This should never be the case but better safe than sorry.
        if (!IsAvailable)
        {
            Log.Error("PerformancePointCalculator: performance-calculator is not available.");
            return false;
        }
        
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        if (File.Exists(beatmapFilePath))
        {
            return true;
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
            Log.Error("PerformancePointCalculator: Failed to download beatmap {BeatmapId}, {e.Message}", beatmapId, e);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the pp for beatmap, has a 5-second timeout.  
    /// </summary>
    private async Task<IPerformanceInfo?> CalculateBeatmapPerformancePoints(int beatmapId,
        int mods = 0,
        int? n300 = null,
        int? n100 = null,
        int? n50 = null,
        int? nMisses = null,
        int? nMaxCombo = null)
    {
        var appName = "performance-calculator" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty);
        var individualPlayMode = nMaxCombo != null;

        var arguments = individualPlayMode
            ? $"{beatmapId} {n300} {n100} {n50} {nMisses} {nMaxCombo} {mods}"
            : $"{beatmapId} {mods}";

        var performanceCalcProcess = RunProcessAsync(appName, arguments);

        if (performanceCalcProcess == null)
        {
            Log.Error("PerformancePointCalculator: Failed to start performance calculator process.");
            return null;
        }

        try
        {
            string output = await performanceCalcProcess.WaitAsync(TimeSpan.FromSeconds(5));

            if (output.StartsWith("err: "))
            {
                Log.Error("PerformancePointCalculator: Failed to calculate pp for beatmap {BeatmapId}, {output}", beatmapId, output);
                return null;
            }

            var values = output.Split('\n');
            if (values.Length < 2)
            {
                Log.Error("PerformancePointCalculator: Failed to calculate pp for beatmap {BeatmapId}, {output}", beatmapId, output);
                return null;
            }

            if (individualPlayMode)
            {
                return new PlayPerformanceInfo()
                {
                    PerformancePoints = (int)Math.Round(Convert.ToDouble(values[0], CultureInfo.InvariantCulture)),
                    MaximumPerformancePoints =
                        (int)Math.Round(Convert.ToDouble(values[1], CultureInfo.InvariantCulture)),
                };
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
            Log.Error("PerformancePointCalculator: Performance calculator process timed out.");
        }
        catch (Exception e)
        {
            Log.Error("PerformancePointCalculator: Error while calculating pp for beatmap {BeatmapId}, {e.Message}", beatmapId, e);
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

            process.Exited += (_, _) =>
            {
                try
                {
                    string stdOut = process.StandardOutput.ReadToEnd();

                    taskCompletionSource.SetResult(stdOut);

                    process.Dispose();
                }
                catch (Exception)
                {
                    Log.Error("PerformancePointCalculator: Failed to read STDOUT");
                }
            };

            process.Start();

            return taskCompletionSource.Task;
        }
        catch (Exception e)
        {
            Log.Error("PerformancePointCalculator: Failed to start process, {e.Message}", e);
            return null;
        }
    }
}