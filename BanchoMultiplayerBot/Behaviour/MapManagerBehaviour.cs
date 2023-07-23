using System.Globalization;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.OsuApi;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using BanchoMultiplayerBot.Utilities;
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
    public int CurrentBeatmapLength { get; private set; }
    public float CurrentBeatmapStarRating { get; private set; }
    public string CurrentBeatmapName { get; private set; } = string.Empty;

    public bool ValidMapPicked { get; private set; } = true;

    public bool IsValidatingMap { get; set; } = false;
    
    private Lobby _lobby = null!;
    private AutoHostRotateBehaviour? _autoHostRotateBehaviour;

    private bool _botAppliedBeatmap;
    private int _lastBotAppliedBeatmap;
    private int _beatmapFallbackId = 2116202; // use the osu! tutorial as default

    private bool _beatmapCheckEnabled = true;

    private int _hostViolationCount = 0;
    private bool _hostValidMapPicked = true;

    private DateTime _matchStartTime = DateTime.Now;
    private DateTime _matchFinishTime = DateTime.Now;
    private DateTime _beatmapRejectTime = DateTime.Now;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnBeatmapChanged += OnBeatmapChanged;
        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        _lobby.MultiplayerLobby.OnMatchAborted += OnMatchFinished;
        
        _lobby.MultiplayerLobby.OnSettingsUpdated += OnSettingsUpdated;
        
        _lobby.OnUserMessage += OnUserMessage;
        _lobby.OnAdminMessage += OnAdminMessage;

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

    private async void OnSettingsUpdated()
    {
        if (!IsValidatingMap)
        {
            return;
        }
        
        // If mods have been changed from Freemod, re-validate star rating with the new mods.
        if (_lobby.MultiplayerLobby.Mods == Mods.Freemod)
        {
            return;
        }
        
        try
        {
            // osu!api has different bits for each mod, so we need to "translate" it.
            // We only really care about the difficulty increasing mods anyway.
            ModsModel osuApiMods = 0;

            if ((_lobby.MultiplayerLobby.Mods & Mods.DoubleTime) != 0 ||
                (_lobby.MultiplayerLobby.Mods & Mods.Nightcore) != 0)
                osuApiMods |= ModsModel.DoubleTime;
            if ((_lobby.MultiplayerLobby.Mods & Mods.HardRock) != 0)
                osuApiMods |= ModsModel.HardRock;

            var beatmapInformation = await _lobby.Bot.OsuApi.GetBeatmapInformation(CurrentBeatmapId, (int)osuApiMods);
            if (beatmapInformation == null)
            {
                return;
            }

            if (!IsAllowedBeatmapStarRating(beatmapInformation))
            {
                Log.Error("Detected an attempt to play a map out of the lobby's star rating! Aborting...");

                _lobby.SendMessage("Detected an attempt to play a map out of the lobby's star rating! Aborting...");
                _lobby.SendMessage("!mp abort");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Exception while re-validating map mods: {e}");
        }
    }

    private void OnMatchFinished()
    {
        IsValidatingMap = false;
        
        _matchFinishTime = DateTime.Now;

        _lobby.Bot.RuntimeInfo.Statistics.MapLength.WithLabels(_lobby.LobbyLabel).Observe(CurrentBeatmapLength);
    }

    private void OnMatchStarted()
    {
        _matchStartTime = DateTime.Now;

        _lobby.Bot.RuntimeInfo.Statistics.MapPlayTime.WithLabels(_lobby.LobbyLabel).Observe((DateTime.Now - _matchFinishTime).TotalSeconds);

        if (EnsureValidMap(true))
            return;

        IsValidatingMap = true;
        
        _lobby.SendMessage("!mp settings");
    }

    private void OnUserMessage(IPrivateIrcMessage msg)
    {
        if (msg.Content.ToLower().EndsWith("!r") || msg.Content.ToLower().StartsWith("!regulations"))
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

        if (msg.Content.ToLower().Equals("!mirror"))
        {
            _lobby.SendMessage($"[https://beatconnect.io/b/{CurrentBeatmapSetId} BeatConnect Mirror] - [https://osu.direct/d/{CurrentBeatmapSetId} osu.direct Mirror]");
        }

        if (msg.Content.ToLower().StartsWith("!timeleft") && _lobby.MultiplayerLobby.MatchInProgress)
        {
            var timeLeft = (_matchStartTime.AddSeconds(CurrentBeatmapLength) - DateTime.Now).ToString(@"mm\:ss");

            _lobby.SendMessage($"Time left of current map: {timeLeft}");
        }
    }

    private void OnAdminMessage(IPrivateIrcMessage msg)
    {
        if (!msg.Content.StartsWith("!togglemapcheck"))
        {
            return;
        }

        _beatmapCheckEnabled = !_beatmapCheckEnabled;

        _lobby.SendMessage((_beatmapCheckEnabled ? "Enabled" : "Disabled") + " beatmap checker!");
    }

    private async void OnBeatmapChanged(BeatmapShell beatmap)
    {
        // Ignore the beatmap change made by the bot.
        if (_botAppliedBeatmap && beatmap.Id == _lastBotAppliedBeatmap)
        {
            _botAppliedBeatmap = false;
            ValidMapPicked = true;
            
            return;
        }

        try
        {
            // Attempt to retrieve beatmap info from the API
            var beatmapInfo = await _lobby.Bot.OsuApi.GetBeatmapInformation(beatmap.Id);

            if (beatmapInfo != null)
            {
                // Make sure we're within limits
                await EnsureBeatmapLimits(beatmapInfo, beatmap.Id);
            }
            else
            {
                // Not going to reset the map here, since in the rare case that the osu!api might have issues, I wouldn't
                // want the lobby to be immediately killed since nobody can pick a new map. We'll just have to trust the hosts
                // to be within limits, and if not, players can always "!skip".
                _lobby.SendMessage($"osu!api error while getting beatmap information, star rating could not be validated.");
            }
        }
        catch (BeatmapNotFoundException)
        {
            SetBeatmap(_beatmapFallbackId);
            
            _lobby.SendMessage($"The beatmap picked is not submitted, please pick another one.");
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
        bool hostIsAdministrator = _lobby.MultiplayerLobby.Host is not null && _lobby.Bot.IsAdministrator(_lobby.MultiplayerLobby.Host.Name);

        if ((IsAllowedBeatmapLength(beatmap) && IsAllowedBeatmapStarRating(beatmap) && IsAllowedBeatmapGameMode(beatmap) && !IsBannedBeatmap(beatmap)) 
            || hostIsAdministrator
            || !_beatmapCheckEnabled)
        {
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial every time.
            _beatmapFallbackId = id;

            CurrentBeatmapName = $"{beatmap.Artist} - {beatmap.Title}";
            CurrentBeatmapId = id;
            CurrentBeatmapLength = beatmap.TotalLength == null
                ? 0
                : int.Parse(beatmap.TotalLength, CultureInfo.InvariantCulture);

            CurrentBeatmapStarRating = beatmap.DifficultyRating == null
                ? 0
                : float.Parse(beatmap.DifficultyRating, CultureInfo.InvariantCulture);
            
            _lobby.Bot.RuntimeInfo.Statistics.MapPickTime.WithLabels(_lobby.LobbyLabel).Observe((DateTime.Now - _matchFinishTime).TotalSeconds);
            
            ValidMapPicked = true;

            if (beatmap.BeatmapsetId != null)
                CurrentBeatmapSetId = int.Parse(beatmap.BeatmapsetId);

            _botAppliedBeatmap = true;
            _lastBotAppliedBeatmap = CurrentBeatmapId;

            _hostValidMapPicked = true;
            
            // By "setting" the map our self directly after the host picked it, 
            // it will automatically be set to the newest version, even if the host's one is outdated.
            _lobby.SendMessage($"!mp map {CurrentBeatmapId} 0");

            await AnnounceNewBeatmap(beatmap, id);
            
            return;
        }
        
        SetBeatmap(_beatmapFallbackId);

        _lobby.Bot.RuntimeInfo.Statistics.MapViolations.WithLabels(_lobby.LobbyLabel).Inc();

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
            var configuredMaxMapLength = TimeSpan.FromSeconds(_lobby.Configuration.MaximumMapLength);
                
            _lobby.SendMessage($"The beatmap you've picked is too long. Max map length: {configuredMaxMapLength:mm\\:ss}");
        }
        else
        {
            if (_lobby.Configuration.LimitStarRating && beatmap.DifficultyRating != null)
            {
                var mapStarRating = Math.Round(float.Parse(beatmap.DifficultyRating, CultureInfo.InvariantCulture), 2);

                _lobby.SendMessage(mapStarRating >= _lobby.Configuration.MaximumStarRating
                    ? $"The selected beatmap's star rating is too high for the lobby ({mapStarRating:0.00} > {_lobby.Configuration.MaximumStarRating:0.0}). Please make sure to use the online star rating!"
                    : $"The selected beatmap's star rating is too low for the lobby ({_lobby.Configuration.MinimumStarRating:0.0} > {mapStarRating:0.00}). Please make sure to use the online star rating!");
            }
            else
            {
                _lobby.SendMessage($"The beatmap you've picked is out of the lobby star range ({_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}*), please make sure to use the online star rating.");
            }
        }

        ValidMapPicked = false;
        
        _hostValidMapPicked = false;
        _beatmapRejectTime = DateTime.Now;

        EnsureValidMap(false);
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

                _lobby.SendMessage($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title} [{beatmapModel.Version ?? string.Empty}]] - ([https://beatconnect.io/b/{CurrentBeatmapSetId} BeatConnect Mirror] - [https://osu.direct/d/{CurrentBeatmapSetId} osu.direct Mirror])");
                
                try
                {
                    if (beatmapModel.DifficultyRating != null)
                    {
                        var starRating = Math.Round(float.Parse(beatmapModel.DifficultyRating, CultureInfo.InvariantCulture), 2);

                        if (_lobby.Bot.PerformancePointCalculator == null)
                        {
                            _lobby.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {timeSpan.ToString(@"mm\:ss")} | BPM: {beatmapModel.Bpm})");
                            _lobby.SendMessage($"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall})");
                        }
                        else
                        {
                            var ppInfo = await _lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(id);

                            _lobby.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {timeSpan.ToString(@"mm\:ss")} | BPM: {beatmapModel.Bpm})");

                            _lobby.SendMessage(ppInfo != null
                                ? $"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp)"
                                : $"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall})");
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
        
        return _lobby.Configuration.MaximumMapLength >= mapLength && mapLength >= _lobby.Configuration.MinimumMapLength;
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

    /// <summary>
    /// This is to prevent the host from cheating the system by starting the match within the small time window while
    /// the bot retrieves map info from the API.
    /// </summary>
    /// <param name="matchStart">If we're calling this from a match start event.</param>
    private bool EnsureValidMap(bool matchStart)
    {
        var now = DateTime.Now;
        
        if (_hostValidMapPicked)
        {
            return false;
        }

        if (matchStart)
        {
            if (3 < (now - _beatmapRejectTime).Duration().TotalSeconds)
            {
                return false;
            }
        }
        else
        {
            if (!_lobby.MultiplayerLobby.MatchInProgress)
            {
                return false;
            }
        
            // At this point we know that we got the API response, and the map is invalid, and the fact that we're playing.
            // If this is due to the API being suupper slow, we'll just let it fly.
            if ((now - _matchStartTime).TotalSeconds > 5)
            {
                return false;
            }
        }

        Log.Error("Detected an attempt to play a map out of the lobby's star rating! Aborting...");

        _lobby.SendMessage("Detected an attempt to play a map out of the lobby's star rating! Aborting...");
        _lobby.SendMessage("!mp abort");

        return true;
    }
}