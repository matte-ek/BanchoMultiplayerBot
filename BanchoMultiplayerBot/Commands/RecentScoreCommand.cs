using System.Text;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using OsuSharp.Enums;

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
            var user = await context.UsingApiClient(async (apiClient) => await apiClient.GetUserAsync(context.Message.Sender));

            if (user == null)
            {
                context.Reply("Unable to find user.");
                return;
            }

            playerId = user.Id;
        }

        var score = (await context.UsingApiClient(async (apiClient) => await apiClient.GetUserScoresAsync(playerId.Value, UserScoreType.Recent, true, true, Ruleset.Osu,
            1)))?.FirstOrDefault();

        if (score == null)
        {
            context.Reply("Unable to find any recent score.");
            return;
        }

        var beatmapInformation = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync(score.Beatmap!.Id));
        if (beatmapInformation == null)
        {
            context.Reply("Unable to find beatmap.");
            return;
        }
        
        var ppInformation = await context.Bot.PerformancePointCalculator!.CalculateScorePerformancePoints(score.Beatmap!.Id, score);
        if (ppInformation == null)
        {
            context.Reply("Unable to calculate performance points.");
            return;
        }
        
        var response = new StringBuilder();

        response.Append($"Recent score for {context.Message.Sender}: ");

        response.Append(
            $"[https://osu.ppy.sh/b/{score.BeatmapSet?.Id} {score.BeatmapSet?.Artist} - {score.BeatmapSet?.Title} [{score.Beatmap?.Version ?? string.Empty}]]");

        if (score.Mods.Length != 0)
        {
            response.Append($" + {string.Join("", score.Mods)}");
        }

        response.Append($" | [{score.Grade.AsHumanString()}]");
                
        response.Append($" | {ppInformation.PerformancePoints} pp ");
                
        if (!score.IsPerfect && ppInformation.MaximumPerformancePoints != ppInformation.PerformancePoints)
        {
            response.Append($"({ppInformation.MaximumPerformancePoints} pp if FC) ");
        }

        response.Append($"| {(score.Accuracy * 100f):0.00}% | ");

        response.Append($"x{score.MaxCombo}/{beatmapInformation.MaxCombo} | {score.Statistics.Count300}/{score.Statistics.Count100}/{score.Statistics.Count50}/{score.Statistics.Misses}");
        
        context.Reply(response.ToString());
    }
}