using System.Text;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using osu.NET.Enums;
using Serilog;

namespace BanchoMultiplayerBot.Commands;

public class RecentScoreCommand : IPlayerCommand
{
    public string Command => "RecentScore";

    public List<string>? Aliases => ["rs"];

    public bool AllowGlobal => true;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public async Task ExecuteAsync(CommandEventContext context)
    {
        int? playerId = context.Player?.Id;

        if (playerId == null)
        {
            var userResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetUserAsync(context.Message.Sender));
            
            if (userResult.IsFailure)
            {
                Log.Error($"API user lookup failed, {userResult.Error}");
                context.Reply("Unable to find user.");
                return;
            }

            playerId = userResult!.Value!.Id;
        }

        var scoresResult = (await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(playerId.Value, UserScoreType.Recent, true, true, Ruleset.Osu, 1)));

        if (scoresResult.IsFailure)
        {
            context.Reply("Unable to find any recent score.");
            return;
        }

        var score = scoresResult.Value!.First();
        
        var beatmapInformationResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync(score.BeatmapId));
        if (beatmapInformationResult.IsFailure)
        {
            Log.Error($"API beatmap lookup failed, {beatmapInformationResult.Error}");
            context.Reply("Unable to find beatmap.");
            return;
        }
        
        var beatmapInformation = beatmapInformationResult.Value!;
        
        var ppInformation = await context.Bot.PerformancePointCalculator!.CalculateScorePerformancePoints(score.BeatmapId, score);
        if (ppInformation == null)
        {
            context.Reply("Unable to calculate performance points.");
            return;
        }
        
        var response = new StringBuilder();

        response.Append($"Recent score for {context.Message.Sender}: ");

        response.Append(
            $"[https://osu.ppy.sh/b/{beatmapInformation.SetId} {beatmapInformation.Set?.Artist} - {beatmapInformation.Set?.Title} [{beatmapInformation.Version}]]");

        if (score.Mods.Length != 0)
        {
            response.Append($" + {string.Join("", score.Mods.Select(m => m.ToString()))}");
        }

        response.Append($" | [{score.Grade.AsHumanString()}]");
                
        response.Append($" | {ppInformation.PerformancePoints} pp ");
                
        if (!score.IsPerfectComboLegacy && ppInformation.MaximumPerformancePoints != ppInformation.PerformancePoints)
        {
            response.Append($"({ppInformation.MaximumPerformancePoints} pp if FC) ");
        }

        response.Append($"| {(score.Accuracy * 100f):0.00}% | ");

        response.Append($"x{score.MaxCombo}/{beatmapInformation.MaxCombo} | {score.Statistics.Good}/{score.Statistics.Ok}/{score.Statistics.Meh}/{score.Statistics.Miss}");
        
        context.Reply(response.ToString());
    }
}