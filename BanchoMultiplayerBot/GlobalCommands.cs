using System.Globalization;
using System.Text;
using BanchoMultiplayerBot.Extensions;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot;

public class GlobalCommands
{
    private Bot _bot;

    public GlobalCommands(Bot bot)
    {
        _bot = bot;
    }

    public void Setup()
    {
        _bot.Client.OnPrivateMessageReceived += OnPrivateMessageReceived;
    }

    private async void OnPrivateMessageReceived(IPrivateIrcMessage msg)
    {
        try
        {
            if (msg.Content.ToLower().Equals("!help") || msg.Content.ToLower().Equals("!info") || msg.Content.ToLower().Equals("!commands"))
            {
                _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient, $"osu! auto host rotation bot (v{Bot.Version}) [https://github.com/matte-ek/BanchoMultiplayerBot/blob/master/COMMANDS.md Help & Commands]");
            }

            if (msg.IsDirect &&
                (msg.Content.StartsWith("!mp create") ||
                 msg.Content.StartsWith("!create") ||
                 msg.Content.StartsWith("!join")))
            {
                _bot.SendMessage(msg.Sender, "I cannot unfortunately join or create any new lobbies.");
            }
            
            if (msg.Content.ToLower().Equals("!rs"))
            {
                if (_bot.PerformancePointCalculator == null)
                {
                    return;
                }
  
                var recentScore = await _bot.OsuApi.GetRecentScore(msg.Sender);
                if (recentScore == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get recent score details.");
                    return;
                }

                var beatmapInformation =
                    await _bot.OsuApi.GetBeatmapInformation(int.Parse(recentScore.BeatmapId!));
                
                if (beatmapInformation == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get beatmap information.");
                    return;
                }

                var ppInformation = await _bot.PerformancePointCalculator!.CalculateScorePerformancePoints(int.Parse(recentScore.BeatmapId!), recentScore);
                if (ppInformation == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get calculate pp information.");
                    return;
                }

                var acc = recentScore.CalculateAccuracy();
                
                var response = new StringBuilder();

                response.Append($"Recent score for {msg.Sender}: {ppInformation.PerformancePoints} pp ");

                if (recentScore.Perfect != "1")
                {
                    response.Append($"({ppInformation.MaximumPerformancePoints} pp if FC) ");
                }

                response.Append($"| {acc:0.00}% | ");
                
                response.Append($"x{recentScore.Maxcombo}/{beatmapInformation.MaxCombo} | {recentScore.Count300}/{recentScore.Count100}/{recentScore.Count50}/{recentScore.Countmiss}");
                
                response.Append($" | [https://osu.ppy.sh/b/{beatmapInformation.BeatmapId} {beatmapInformation.Artist} - {beatmapInformation.Title} [{beatmapInformation.Version ?? string.Empty}]] ");
                
                if (recentScore.EnabledMods != "0")
                {
                    response.Append($" + {((Mods)int.Parse(recentScore.EnabledMods!)).ToAbbreviatedForm()}");
                }
                
                _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient, response.ToString());
            }
            
            // This is more or less out of scope for this bot, but I've added it anyway for DMs
            // however it's not documented in COMMANDS.md.
            if ((msg.Content.ToLower().Equals("!map") || msg.Content.ToLower().Equals("!m")) && msg.IsDirect)
            {
                if (_bot.PerformancePointCalculator == null)
                {
                    return;
                }
  
                var recentScore = await _bot.OsuApi.GetRecentScore(msg.Sender);
                if (recentScore == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get recent score details.");
                    return;
                }

                var beatmapInformation =
                    await _bot.OsuApi.GetBeatmapInformation(int.Parse(recentScore.BeatmapId!));
                
                if (beatmapInformation == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get beatmap information.");
                    return;
                }

                var ppInformation = await _bot.PerformancePointCalculator!.CalculatePerformancePoints(int.Parse(recentScore.BeatmapId!));
                if (ppInformation == null)
                {
                    _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,"Unable to get calculate pp information.");
                    return;
                }

                var starRating = Math.Round(float.Parse(beatmapInformation.DifficultyRating!, CultureInfo.InvariantCulture), 2);
                var length = TimeSpan.FromSeconds(int.Parse(beatmapInformation.TotalLength ?? "0", CultureInfo.InvariantCulture));
                
                _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient, $"[https://osu.ppy.sh/b/{recentScore.BeatmapId} {beatmapInformation.Artist} - {beatmapInformation.Title} [{beatmapInformation.Version ?? string.Empty}]]");
                _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient,$"(Star Rating: {starRating:.0#} | {beatmapInformation.GetStatusString()} | Length: {length:mm\\:ss} | BPM: {beatmapInformation.Bpm})");
                _bot.SendMessage(msg.IsDirect ? msg.Sender : msg.Recipient, $"(AR: {beatmapInformation.DiffApproach} | CS: {beatmapInformation.DiffSize} | OD: {beatmapInformation.DiffOverall} | HP: {beatmapInformation.DiffDrain} | 100%: {ppInformation.Performance100}pp | 98%: {ppInformation.Performance98}pp | 95%: {ppInformation.Performance95}pp)");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Exception at global commands: {e}");
        }
    }
}