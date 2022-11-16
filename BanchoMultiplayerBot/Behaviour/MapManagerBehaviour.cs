using System.Globalization;
using BanchoMultiplayerBot.OsuApi;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will be responsible for making sure
/// the map picked by the host is within the limit set
/// in the configuration.
/// </summary>
public class MapManagerBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private bool _botAppliedBeatmap;
    private int _lastBotAppliedBeatmap;

    private int _beatmapFallbackId = 2116202;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
        _lobby.OnUserMessage += OnUserMessage; 
    }

    private async void OnUserMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.StartsWith("!r") || msg.Content.StartsWith("!regulations"))
        {
            var timeSpan = TimeSpan.FromSeconds(_lobby.Configuration.MaximumMapLength);
            
            await _lobby.SendMessageAsync($"Star rating: {_lobby.Configuration.MinimumStarRating:.0#} - {_lobby.Configuration.MaximumStarRating:.0#}, max length: {timeSpan.ToString(@"mm\:ss\:fff")}");
        }
    }

    private async void OnBeatmapChanged(BeatmapShell beatmap)
    {
        // Ignore the beatmap change made by the bot.
        if (_botAppliedBeatmap && beatmap.Id == _lastBotAppliedBeatmap)
        {
            _botAppliedBeatmap = false;
            
            return;
        }
        
        // TODO: Figure out some clever way to deal with double time, as it's
        // the only global mod that should matter here. For now we're ignoring
        // mods completely.

        try
        {
            // Attempt to retrieve beatmap info from the API
            var beatmapInfo = await _lobby.Bot.OsuApi.GetBeatmapInformation(beatmap.Id);

            if (beatmapInfo != null)
            {
                // Make sure we're within limits
                await EnsureBeatmapLimits(beatmapInfo, beatmap.Id);
            }
        }
        catch (BeatmapNotFoundException)
        {
            await _lobby.SendMessageAsync($"Only submitted beat maps are allowed.");
        }
        catch (ApiKeyInvalidException)
        {
            await _lobby.SendMessageAsync($"Internal error while getting beatmap information, please try again.");
        }
        catch (Exception)
        {
            await _lobby.SendMessageAsync($"Internal error while getting beatmap information, please try again.");
        }
    }

    private async Task EnsureBeatmapLimits(BeatmapModel beatmap, int id)
    {
        if (IsAllowedBeatmapLength(beatmap) && IsAllowedBeatmapStarRating(beatmap))
        {
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial everytime.
            _beatmapFallbackId = id;

            await AnnounceNewBeatmap(beatmap, id);
            
            return;
        }

        await SetBeatmap(_beatmapFallbackId);
        
        if (!IsAllowedBeatmapLength(beatmap))
        {
            await _lobby.SendMessageAsync($"The beatmap you've picked is too long/short, please pick another one.");
        }
        else
        {
            await _lobby.SendMessageAsync($"The beatmap you've picked is out of the lobby star range, please pick another one.");
        }
    }

    private async Task SetBeatmap(int id)
    {
        _botAppliedBeatmap = true;
        _lastBotAppliedBeatmap = id;
        
        await _lobby.SendMessageAsync($"!mp map {id} 0");
    }

    private async Task AnnounceNewBeatmap(BeatmapModel beatmapModel, int id)
    {
        await _lobby.SendMessageAsync($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title}] - ([https://beatconnect.io/b/{id} Mirror])");
    }
    
    private bool IsAllowedBeatmapStarRating(BeatmapModel beatmap)
    {
        if (!_lobby.Configuration.LimitStarRating)
            return true;
        if (beatmap.DifficultyRating == null)
            return false;
        
        var config = _lobby.Configuration;
        var minRating = config.MinimumStarRating;
        var maxRating = config.MaximumStarRating;
        
        if (config.StarRatingErrorMargin != null)
        {
            minRating -= config.StarRatingErrorMargin.Value;
            maxRating += config.StarRatingErrorMargin.Value;
        }

        var mapStarRating = float.Parse(beatmap.DifficultyRating, CultureInfo.InvariantCulture);

        return maxRating >= mapStarRating && mapStarRating >= minRating;
    }

    private bool IsAllowedBeatmapLength(BeatmapModel beatmap)
    {
        return true;
    }
}