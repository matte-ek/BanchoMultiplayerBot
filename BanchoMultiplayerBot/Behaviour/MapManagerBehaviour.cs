using BanchoMultiplayerBot.OsuApi;
using BanchoMultiplayerBot.OsuApi.Exceptions;
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
        await _lobby.SendMessageAsync($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title}] ([https://beatconnect.io/b/{id} Mirror])");
    }
    
    private bool IsAllowedBeatmapStarRating(BeatmapModel beatmap)
    {
        if (!_lobby.Configuration.LimitStarRating)
            return true;
        if (beatmap.DifficultyRating == null)
            return false;
        
        var config = _lobby.Configuration;

        float minRating = config.MinimumStarRating;
        float maxRating = config.MaximumStarRating;
        
        if (config.StarRatingErrorMargin != null)
        {
            minRating -= config.StarRatingErrorMargin.Value;
            maxRating += config.StarRatingErrorMargin.Value;
        }

        float mapStarRating = float.Parse(beatmap.DifficultyRating);


        return true;
    }

    private bool IsAllowedBeatmapLength(BeatmapModel beatmap)
    {
        return true;
    }
}