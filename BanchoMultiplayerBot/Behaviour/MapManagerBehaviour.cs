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
    public event Action? OnNewAllowedMap; 

    private Lobby _lobby = null!;
    
    private bool _botAppliedBeatmap;
    private int _lastBotAppliedBeatmap;
    private int _beatmapFallbackId = 2116202;

    private int _beatmapPickViolations;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnHostChanged += player =>
        {
            _beatmapPickViolations = 0;
        };
        
        _lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
        _lobby.OnUserMessage += OnUserMessage; 
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.StartsWith("!r") || msg.Content.StartsWith("!regulations"))
        {
            var timeSpan = TimeSpan.FromSeconds(_lobby.Configuration.MaximumMapLength);
            
            _lobby.SendMessage($"Star rating: {_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}* | Max length: {timeSpan.ToString(@"mm\:ss")}");
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
                EnsureBeatmapLimits(beatmapInfo, beatmap.Id);
            }
        }
        catch (BeatmapNotFoundException)
        {
            _lobby.SendMessage($"Only submitted beat maps are allowed.");
        }
        catch (ApiKeyInvalidException)
        {
            _lobby.SendMessage($"Internal error while getting beatmap information, please try again.");
        }
        catch (Exception)
        {
            _lobby.SendMessage($"Internal error while getting beatmap information, please try again.");
        }
    }

    private void EnsureBeatmapLimits(BeatmapModel beatmap, int id)
    {
        if (IsAllowedBeatmapLength(beatmap) && IsAllowedBeatmapStarRating(beatmap))
        {
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial everytime.
            _beatmapFallbackId = id;

            AnnounceNewBeatmap(beatmap, id);
            
            return;
        }

        SetBeatmap(_beatmapFallbackId);
        
        if (!IsAllowedBeatmapLength(beatmap))
        {
            _lobby.SendMessage($"The beatmap you've picked is too long, please pick another one.");
        }
        else
        {
            _lobby.SendMessage($"The beatmap you've picked is out of the lobby star range, please pick another one. ({_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}*)");
        }

        _beatmapPickViolations++;
    }

    private void SetBeatmap(int id)
    {
        _botAppliedBeatmap = true;
        _lastBotAppliedBeatmap = id;
        
        _lobby.SendMessage($"!mp map {id} 0");
    }

    private void AnnounceNewBeatmap(BeatmapModel beatmapModel, int id)
    {
        try
        {
            if (beatmapModel.TotalLength != null)
            {
                var timeSpan = TimeSpan.FromSeconds(int.Parse(beatmapModel.TotalLength));

                _lobby.SendMessage($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title}] (BPM: {beatmapModel.Bpm} | Length: {timeSpan.ToString(@"mm\:ss")}) - ([https://beatconnect.io/b/{id} Mirror])");
                
                OnNewAllowedMap?.Invoke();
            }
        }
        catch (Exception)
        {
            // ignored
        }
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
        if (!_lobby.Configuration.LimitMapLength)
            return true;
        if (beatmap.TotalLength == null)
            return false;
        
        var mapLength = int.Parse(beatmap.TotalLength, CultureInfo.InvariantCulture);
        
        return _lobby.Configuration.MaximumMapLength >= mapLength && mapLength >= _lobby.Configuration.MinimumMapLength ;
    }
}