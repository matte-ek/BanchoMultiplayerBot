using System.Globalization;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.OsuApi;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using BanchoSharp;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will be responsible for making sure
/// the map picked by the host is within the limit set
/// in the configuration.
/// </summary>
public class MapManagerBehaviour : IBotBehaviour
{
    public event Action? OnNewAllowedMap;

    public int CurrentBeatmapSetId { get; private set; }
    public int CurrentBeatmapId { get; private set; }
    public string CurrentBeatmapName { get; private set; } = string.Empty;

    private Lobby _lobby = null!;
    private AutoHostRotateBehaviour? _autoHostRotateBehaviour;

    private bool _botAppliedBeatmap;
    private int _lastBotAppliedBeatmap;
    private int _beatmapFallbackId = 2116202; // use the osu! tutorial as default

    private int _hostViolationCount = 0;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
        _lobby.OnUserMessage += OnUserMessage;

        _lobby.MultiplayerLobby.OnHostChanged += player =>
        {
            _hostViolationCount = 0;
        };
        
        var autoHostRotateBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour));
        if (autoHostRotateBehaviour != null)
        {
            _autoHostRotateBehaviour = (AutoHostRotateBehaviour)autoHostRotateBehaviour;
        }
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.EndsWith("!r") || msg.Content.StartsWith("!regulations"))
        {
            var timeSpan = TimeSpan.FromSeconds(_lobby.Configuration.MaximumMapLength);

            var requiredModeName = _lobby.Configuration.Mode switch
            {
                GameMode.osu => "osu!std",
                GameMode.osuCatch => "osu!catch",
                GameMode.osuMania => "osu!mania",
                GameMode.osuTaiko => "osu!taiko",
                _ => "Any Mode"
            };

            _lobby.SendMessage($"Star rating: {_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}* | Max length: {timeSpan.ToString(@"mm\:ss")} | {requiredModeName}");
        }

        if (msg.Content.Equals("!mirror"))
        {
            _lobby.SendMessage($"[https://beatconnect.io/b/{CurrentBeatmapSetId} Mirror Download]");
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
            SetBeatmap(_beatmapFallbackId);
            
            _lobby.SendMessage($"Only submitted beat maps are allowed.");
        }
        catch (ApiKeyInvalidException)
        {
            Log.Error("API Key invalid exception while trying to retrive map details.");

            _lobby.SendMessage($"Internal error while getting beatmap information, please try again.");
        }
        catch (Exception e)
        {
            Log.Error($"Exception while handing beatmap change: {e.Message}");

            _lobby.SendMessage($"Internal error while getting beatmap information, please try again.");
        }
    }

    private async Task EnsureBeatmapLimits(BeatmapModel beatmap, int id)
    {
        if (IsAllowedBeatmapLength(beatmap) && IsAllowedBeatmapStarRating(beatmap) && IsAllowedBeatmapGameMode(beatmap) && !IsBannedBeatmap(beatmap))
        {
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial everytime.
            _beatmapFallbackId = id;

            CurrentBeatmapName = $"{beatmap.Artist} - {beatmap.Title}";
            CurrentBeatmapId = id;
            
            if (beatmap.BeatmapsetId != null)
                CurrentBeatmapSetId = int.Parse(beatmap.BeatmapsetId);

            _botAppliedBeatmap = true;
            _lastBotAppliedBeatmap = CurrentBeatmapId;
            
            // By "setting" the map ourself directly after the host picked it, 
            // it will automatically be set to the newest version, even if the host's one is outdated.
            _lobby.SendMessage($"!mp map {CurrentBeatmapId} 0");

            await AnnounceNewBeatmap(beatmap, id);
            
            return;
        }
        
        SetBeatmap(_beatmapFallbackId);
        
        if (IsBannedBeatmap(beatmap))
        {
            _lobby.SendMessage(beatmap.Title != null
                ? $"This map set ({beatmap.Title}) has been banned."
                : "This map set has been banned.");
        }
        else if (!IsAllowedBeatmapGameMode(beatmap))
        {
            string modeName = _lobby.Configuration.Mode switch
            {
                GameMode.osu => "osu!std",
                GameMode.osuCatch => "osu!catch",
                GameMode.osuMania => "osu!mania",
                GameMode.osuTaiko => "osu!taiko",
                _ => "Error"
            };

            _lobby.SendMessage($"Please only pick beatmaps from the game mode {modeName}.");
        }
        else if (!IsAllowedBeatmapLength(beatmap))
        {
            _lobby.SendMessage($"The beatmap you've picked is too long, please pick another one.");
        }
        else
        {
            _lobby.SendMessage($"The beatmap you've picked is out of the lobby star range ({_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}*), please make sure to use the online star rating.");
        }
        
        RunViolationAutoSkip();
    }

    private void SetBeatmap(int id)
    {
        _botAppliedBeatmap = true;
        _lastBotAppliedBeatmap = id;
        
        _lobby.SendMessage($"!mp map {id} 0");
    }

    private async Task AnnounceNewBeatmap(BeatmapModel beatmapModel, int id)
    {
        try
        {
            if (beatmapModel.TotalLength != null)
            {
                var timeSpan = TimeSpan.FromSeconds(int.Parse(beatmapModel.TotalLength));

                _lobby.SendMessage($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title}] - ([https://beatconnect.io/b/{CurrentBeatmapSetId} Mirror])");

                try
                {
                    if (beatmapModel.DifficultyRating != null)
                    {
                        float starRating = float.Parse(beatmapModel.DifficultyRating, CultureInfo.InvariantCulture);

                        if (_lobby.Bot.PerformancePointCalculator == null)
                        {
                            
                            _lobby.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {timeSpan.ToString(@"mm\:ss")} | BPM: {beatmapModel.Bpm})");
                        }
                        else
                        {
                            var ppInfo = await _lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(id);

                            _lobby.SendMessage(ppInfo != null
                                ? $"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {timeSpan.ToString(@"mm\:ss")} | BPM: {beatmapModel.Bpm} | 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp)"
                                : $"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {timeSpan.ToString(@"mm\:ss")} | BPM: {beatmapModel.Bpm}");
                        }
                    }
                }

                catch (Exception)
                {
                    // ignored, used to catch weird edge cases within the API
                }
                
                OnNewAllowedMap?.Invoke();
            }
        }
        catch (Exception)
        {
            // ignored, used to catch weird edge cases within the API
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

    private bool IsAllowedBeatmapGameMode(BeatmapModel beatmap)
    {
        if (_lobby.Configuration.Mode == null)
            return true;
        
        // Game modes are defined in the API as:
        // 0 - osu!standard
        // 1 - osu!taiko
        // 2 - osu!catch
        // 3 - osu!mania
        
        string? beatmapMode = beatmap.Mode;

        if (beatmapMode == null)
        {
            Log.Error($"No beatmap mode for map {beatmap.BeatmapId}");
            return false;
        }

        return _lobby.Configuration.Mode switch
        {
            GameMode.osu => beatmapMode == "0",
            GameMode.osuTaiko => beatmapMode == "1",
            GameMode.osuCatch => beatmapMode == "2",
            GameMode.osuMania => beatmapMode == "3",
            _ => false
        };
    }

    private bool IsBannedBeatmap(BeatmapModel beatmap)
    {
        var config = _lobby.Bot.Configuration;
        
        if (config.BannedBeatmaps == null)
            return false;
        if (beatmap.BeatmapsetId == null)
            return false;

        try
        {
            return int.TryParse(beatmap.BeatmapsetId, out int beatmapSetId) && config.BannedBeatmaps.ToList().Contains(beatmapSetId);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private void RunViolationAutoSkip()
    {
        if (_lobby.Configuration.AutomaticallySkipAfterViolations == null ||
            _lobby.Configuration.ViolationSkipCount == null ||
            _autoHostRotateBehaviour == null)
            return;

        if (!_lobby.Configuration.AutomaticallySkipAfterViolations.Value)
            return;
        
        _hostViolationCount++;
        
        int skipViolationCount = _lobby.Configuration.ViolationSkipCount.Value;
        if (_hostViolationCount >= skipViolationCount)
        {
            _hostViolationCount = 0;

            _lobby.SendMessage($"Skipping host automatically due to {skipViolationCount} violations.");
            
            _autoHostRotateBehaviour.SkipCurrentHost();
        }
    }
}