using System.Diagnostics;
using System.Net;
using BanchoMultiplayerBot.Data;
using Serilog;
using System.Text.Json;

namespace BanchoMultiplayerBot.OsuApi;

public static class PerformanceCalculator
{
    private static readonly HttpClient HttpClient = new HttpClient();
    private const string OsuToolsDirectory = "../osu-pp-tools";

    public static async Task<BeatmapPerformanceInfo?> CalculatePerformancePoints(int beatmapId)
    {
        const string cacheDir = $"{OsuToolsDirectory}/cache";
        var beatmapFilePath = $"{cacheDir}/{beatmapId}.osu";
        
        if (!Directory.Exists(cacheDir))
        {
            Log.Warning("Failed to find osu tools cache directory.");
            return null;
        }
     
        // Download beatmap (if necessary)
        if (!File.Exists(beatmapFilePath))
        {
            try
            {
                var downloadTask = HttpClient.GetStreamAsync(new Uri($"https://osu.ppy.sh/osu/{beatmapId}"));

                await using var s = await downloadTask.WaitAsync(TimeSpan.FromSeconds(5));
                await using var fs = new FileStream(beatmapFilePath, FileMode.CreateNew);
                
                await s.CopyToAsync(fs);
            }
            catch (Exception e)
            {
                Log.Error($"Error while downloading beatmap {beatmapId} ({e.Message})!");
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
                Log.Error($"Error while calculating pp for map (stage 3) for beatmap {beatmapId}");
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
            Log.Error($"Error while calculating pp for map (stage 0) for beatmap {beatmapId} ({e.Message})");
            return null;
        }
    }

    private static async Task<int?> CalculateBeatmapPerformancePoints(int beatmapId, int acc)
    {
        var performanceCalcProcess = RunProcessAsync($"dotnet", $"../osu-pp-tools/PerformanceCalculator.dll simulate osu -a {acc} -j ../osu-pp-tools/cache/{beatmapId}.osu");

        try
        {
            string output = await performanceCalcProcess.WaitAsync(TimeSpan.FromSeconds(3));
        
            Console.WriteLine(output);
            
            // perfect output check
            if (!(output.StartsWith("{") && output.EndsWith("}")))
            {
                Log.Error($"Error while calculating pp for map (stage 1) {beatmapId}");
                return null;
            }

            dynamic performanceData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(output)!;

            double pp = performanceData.performance_attributes.pp;

            return Convert.ToInt32(Math.Round(pp));
        }
        catch (Exception e)
        {
            Log.Error($"Error while calculating pp for map (stage 2) for beatmap {beatmapId}");
        }

        return null;
    }
    
    private static Task<string> RunProcessAsync(string cmd, string arguments)
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