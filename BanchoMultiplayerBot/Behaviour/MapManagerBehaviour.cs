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
        
        lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
    }

    private void OnBeatmapChanged(BeatmapShell beatmap)
    {
        // Ignore the beatmap change made by the bot.
        if (_botAppliedBeatmap && beatmap.Id == _lastBotAppliedBeatmap)
        {
            _botAppliedBeatmap = false;
            
            return;
        }
        
        // TODO: use the osu! API to get the star rating and length of the map
    }

    private async Task EnsureBeatmapLimits(int id)
    {
        if (IsAllowedBeatmapLength() && IsAllowedBeatmapStarRating())
        {
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial everytime.
            _beatmapFallbackId = id;

            await AnnounceNewBeatmap();
            
            return;
        }
        
        await SetBeatmap(_beatmapFallbackId);

        // TODO: Improve these messages, to more clearly specify what's wrong with the beatmap.
        
        if (!IsAllowedBeatmapLength())
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

    private async Task AnnounceNewBeatmap()
    {
        await _lobby.SendMessageAsync($"Beatmap: ");
    }
    
    private bool IsAllowedBeatmapStarRating()
    {
        return false;
    }

    private bool IsAllowedBeatmapLength()
    {
        return false;
    }
}