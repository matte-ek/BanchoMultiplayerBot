using System.Globalization;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.OsuApi;
using BanchoMultiplayerBot.OsuApi.Exceptions;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will be responsible for making sure
/// the map picked by the host is within the limit set
/// in the configuration.
///
/// TODO: This class is in need of heavy refactoring, as it's getting quite messy with the new features hacked together.
/// </summary>
public class MapManagerBehaviour : IBotBehaviour
{
    public BeatmapInfo? CurrentBeatmap { get; private set; }
    
    public event Action? OnNewAllowedMap;

    internal MapValidation MapValidationStatus { get; set; } = MapValidation.None;
    internal bool HasValidMapPicked { get; private set; } = true;

    private bool _botAppliedBeatmap;
    private int _lastBotAppliedBeatmap;
    private int _beatmapFallbackId = 2116202; // use the osu! tutorial as default

    private bool _beatmapCheckEnabled = true;

    private int _hostViolationCount;
    private bool _hostValidMapPicked = true;

    private DateTime _matchStartTime = DateTime.Now;
    private DateTime _matchFinishTime = DateTime.Now;
    private DateTime _beatmapRejectTime = DateTime.Now;

    private MapValidator _mapValidator = null!;
    
    private BeatmapModel? _currentValidationBeatmap;
    private BeatmapModel? _currentValidationBeatmapDt;

    private List<string> _mapFinishPingList = new();

    private Lobby _lobby = null!;
    private AutoHostRotateBehaviour? _autoHostRotateBehaviour;
    
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

        _mapValidator = new MapValidator(lobby);
    }

    private async void OnSettingsUpdated()
    {
        if (MapValidationStatus == MapValidation.None)
        {
            return;
        }
        
        try
        {
            if (MapValidationStatus == MapValidation.PostStart)
            {
                if (CurrentBeatmap == null)
                {
                    return;
                }
                
                // osu!api has different bits for each mod, so we need to "translate" it.
                // We only really care about the difficulty increasing mods anyway.
                ModsModel osuApiMods = 0;

                if ((_lobby.MultiplayerLobby.Mods & Mods.DoubleTime) != 0 ||
                    (_lobby.MultiplayerLobby.Mods & Mods.Nightcore) != 0)
                    osuApiMods |= ModsModel.DoubleTime;
                if ((_lobby.MultiplayerLobby.Mods & Mods.HardRock) != 0)
                    osuApiMods |= ModsModel.HardRock;

                var mapValid = true;

                if (_lobby.Configuration.AllowDoubleTime == false)
                {
                    if ((osuApiMods & ModsModel.DoubleTime) != 0)
                    {
                        mapValid = false;
                    }
                }
                
                var beatmapInformation = await _lobby.Bot.OsuApi.GetBeatmapInformation(CurrentBeatmap.Id, (int)osuApiMods);
                if (beatmapInformation != null)
                {
                    if (await _mapValidator.ValidateBeatmap(beatmapInformation) != MapValidator.MapStatus.Ok)
                    {
                        mapValid = false;
                    }
                }
                
                if (mapValid)
                {
                    return;
                }
                
                Log.Error("Detected an attempt to play a map out of the lobby's star rating! Aborting...");

                _lobby.SendMessage("Detected an attempt to play a map out of the lobby's star rating! Aborting...");
                _lobby.SendMessage("!mp abort");
            }
            else
            {
                // Should never happen, hopefully.
                if (_currentValidationBeatmap == null ||
                    _currentValidationBeatmapDt == null)
                {
                    Log.Error("Beatmap information null during beatmap validation.");
                    return;
                }
                
                var doubleTimeEnabled = 
                    (_lobby.MultiplayerLobby.Mods & Mods.DoubleTime) != 0 ||
                    (_lobby.MultiplayerLobby.Mods & Mods.Nightcore) != 0;

                await EnforceBeatmapLimits(_currentValidationBeatmap, doubleTimeEnabled ? MapValidator.MapStatus.Ok : MapValidator.MapStatus.StarRating);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Exception while re-validating map mods: {e}");
        }
    }

    private void OnMatchFinished()
    {
        MapValidationStatus = MapValidation.None;
        
        _matchFinishTime = DateTime.Now;

        if (CurrentBeatmap != null)
        {
            _lobby.Bot.RuntimeInfo.Statistics.MapLength.WithLabels(_lobby.LobbyLabel).Observe(CurrentBeatmap.Length.TotalSeconds);
        }

        foreach (var player in _mapFinishPingList)
        {
            _lobby.Bot.SendMessage(player, $"Match has finished in the {_lobby.Configuration.Name} lobby!");
        }
        
        _mapFinishPingList.Clear();
    }

    private void OnMatchStarted()
    {
        _matchStartTime = DateTime.Now;

        _lobby.Bot.RuntimeInfo.Statistics.MapPlayTime.WithLabels(_lobby.LobbyLabel).Observe((DateTime.Now - _matchFinishTime).TotalSeconds);

        if (EnsureValidMap(true))
            return;

        MapValidationStatus = MapValidation.PostStart;
        
        _lobby.UpdateSettings();
    }

    private async void OnUserMessage(PlayerMessage msg)
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

            msg.Reply($"Star rating: {_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}* | Max length: {timeSpan.ToString(@"mm\:ss")} | {requiredModeName}");
        }

        if (CurrentBeatmap == null)
        {
            return;
        }
        
        if (msg.Content.ToLower().Equals("!mirror"))
        {
            msg.Reply($"[https://beatconnect.io/b/{CurrentBeatmap.SetId} BeatConnect Mirror] - [https://osu.direct/d/{CurrentBeatmap.SetId} osu.direct Mirror]");
        }

        // Shows the user the estimated time left of the current map.
        if (msg.Content.ToLower().StartsWith("!timeleft") && _lobby.MultiplayerLobby.MatchInProgress)
        {
            try
            {
                var pingEnabled = false;
                
                if (msg.Content.ToLower().StartsWith("!timeleft ping"))
                {
                    _mapFinishPingList.Add(msg.Sender);
                    pingEnabled = true;
                }
                
                var finishTime = _matchStartTime.Add(CurrentBeatmap.Length);

                // Add a few seconds to account for people loading/finishing the map
                finishTime = finishTime.AddSeconds(15);
                
                // If we know where the map starts, we can be a bit more clever and try to account for that.
                // This obviously assumes that the players skip, and skip at around 50% of that time.
                var beginSkipTime = await BeatmapParser.GetBeatmapStartTime(CurrentBeatmap.Id);
                if (beginSkipTime != null)
                {
                    finishTime = finishTime.Subtract(TimeSpan.FromSeconds(beginSkipTime.Value * 0.5));
                }
            
                var timeLeft = (finishTime - DateTime.Now).ToString(@"mm\:ss");

                msg.Reply(pingEnabled
                    ? $"Estimated time left of current map: {timeLeft}, you will be notified when the map is finished."
                    : $"Estimated time left of current map: {timeLeft}");

                if (beginSkipTime == null)
                    Log.Warning("Unable to get map begin time during !timeleft estimation.");
            }
            catch (Exception)
            {
                // ignored.
            }
        }
    }

    private void OnAdminMessage(PlayerMessage msg)
    {
        if (!msg.Content.StartsWith("!togglemapcheck"))
        {
            return;
        }

        _beatmapCheckEnabled = !_beatmapCheckEnabled;

        msg.Reply((_beatmapCheckEnabled ? "Enabled" : "Disabled") + " beatmap checker!");
    }

    private async void OnBeatmapChanged(BeatmapShell beatmap)
    {
        if (_lobby.IsRecovering)
        {
            return;
        }
        
        // Ignore the beatmap change made by the bot.
        if (_botAppliedBeatmap && beatmap.Id == _lastBotAppliedBeatmap)
        {
            _botAppliedBeatmap = false;
            HasValidMapPicked = true;
            
            return;
        }

        try
        {
            // Attempt to retrieve beatmap info from the API
            var beatmapInfo = await _lobby.Bot.OsuApi.GetBeatmapInformation(beatmap.Id);

            if (beatmapInfo != null)
            {
                var mapStatus = await _mapValidator.ValidateBeatmap(beatmapInfo);

                // If we're below the star rating, the user might have tried to play DT, so if the map is in within the star rating limit
                // with DT enabled, we'll do an extra "!mp settings" check and go from there.
                if (mapStatus == MapValidator.MapStatus.StarRating &&
                    beatmapInfo.DifficultyRating != null &&
                    float.TryParse(beatmapInfo.DifficultyRating, NumberStyles.Float, new CultureInfo("en-US"), out var diffRating) &&
                    _lobby.Configuration.MinimumStarRating >= Math.Round(diffRating, 2))
                {
                    var dtBeatmapInfo = await _lobby.Bot.OsuApi.GetBeatmapInformation(beatmap.Id, (int)ModsModel.DoubleTime);
                    
                    if (dtBeatmapInfo != null && 
                        await _mapValidator.ValidateBeatmap(dtBeatmapInfo) == MapValidator.MapStatus.Ok)
                    {
                        _currentValidationBeatmap = beatmapInfo;
                        _currentValidationBeatmapDt = dtBeatmapInfo;
                        
                        MapValidationStatus = MapValidation.MapPick;

                        _lobby.UpdateSettings();
                    }
                    else
                    {
                        await EnforceBeatmapLimits(beatmapInfo, mapStatus);
                    }
                }
                else
                {
                    await EnforceBeatmapLimits(beatmapInfo, mapStatus);   
                }
            }
            else
            {
                // Not going to reset the map here, since in the rare case that the osu!api might have issues, I wouldn't
                // want the lobby to be immediately killed since nobody can pick a new map. We'll just have to trust the hosts
                // to be within limits, and if not, players can always "!skip".
                _lobby.SendMessage(
                    $"osu!api error while getting beatmap information.");
            }
        }
        catch (BeatmapNotFoundException)
        {
            SetBeatmap(_beatmapFallbackId);

            _lobby.SendMessage($"The beatmap picked is not submitted, please pick another one.");
        }
        catch (ApiKeyInvalidException)
        {
            Log.Error("API Key invalid exception while trying to retrieve map details.");

            // This is ping worthy as it's probably something that needs my attention.
            if (_lobby.Bot.WebhookConfigured)
            {
                _ = WebhookUtils.SendWebhookMessage(_lobby.Bot.Configuration.WebhookUrl!, "API Error",
                    $"osu!api request failed due to invalid key");
            }

            _lobby.SendMessage($"Internal error while getting beatmap information.");
        }
        catch (HttpRequestException e)
        {
            Log.Error($"Exception while sending API request: {e.Message}");

            _lobby.SendMessage($"osu!api timeout while getting beatmap information.");
        }
        catch (Exception e)
        {
            Log.Error($"Exception while handing beatmap change: {e.Message}");

            _lobby.SendMessage($"Internal error while getting beatmap information, please try again.");
        }
    }

    private async Task EnforceBeatmapLimits(BeatmapModel beatmap, MapValidator.MapStatus mapStatus)
    {
        if (mapStatus == MapValidator.MapStatus.Ok)
        {
            var id = int.Parse(beatmap.BeatmapId!);
            
            CurrentBeatmap = new BeatmapInfo()
            {
                Id = id,
                SetId = int.Parse(beatmap.BeatmapsetId ?? "0"),
                Name = $"{beatmap.Artist} - {beatmap.Title}",
                Length = TimeSpan.FromSeconds(int.Parse(beatmap.TotalLength ?? "0", CultureInfo.InvariantCulture)),
                DrainLength = TimeSpan.FromSeconds(int.Parse(beatmap.HitLength ?? "0", CultureInfo.InvariantCulture)),
                StarRating = float.Parse(beatmap.DifficultyRating ?? "0", CultureInfo.InvariantCulture)
            };
            
            HasValidMapPicked = true;
            _hostValidMapPicked = true;
            
            // Update the fallback id whenever someone picks a map that's 
            // within limits, so we don't have to reset to the osu!tutorial every time.
            _beatmapFallbackId = id;

            _lobby.Bot.RuntimeInfo.Statistics.MapPickTime.WithLabels(_lobby.LobbyLabel).Observe((DateTime.Now - _matchFinishTime).TotalSeconds);

            await AnnounceNewBeatmap(beatmap, id);
            
            return;
        }
        
        SetBeatmap(_beatmapFallbackId);
        
        if (mapStatus == MapValidator.MapStatus.Banned)
        {
            _lobby.SendMessage(beatmap.Title != null
                ? $"This map set ({beatmap.Title}) has been banned."
                : "This map set has been banned.");
        }
        else if (mapStatus == MapValidator.MapStatus.GameMode)
        {
            var modeName = _lobby.Configuration.Mode switch
            {
                GameMode.osu => "osu!std",
                GameMode.osuCatch => "osu!catch",
                GameMode.osuMania => "osu!mania",
                GameMode.osuTaiko => "osu!taiko",
                _ => "Error"
            };

            _lobby.SendMessage($"Please only pick beatmaps from the game mode {modeName}.");
        }
        else if (mapStatus == MapValidator.MapStatus.Length)
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
                    ? $"The selected beatmap's star rating is too high for the lobby ({mapStarRating:0.00} > {_lobby.Configuration.MaximumStarRating:0.0})."
                    : $"The selected beatmap's star rating is too low for the lobby ({_lobby.Configuration.MinimumStarRating:0.0} > {mapStarRating:0.00}).");
            }
            else
            {
                _lobby.SendMessage($"The beatmap you've picked is out of the lobby star range ({_lobby.Configuration.MinimumStarRating:.0#}* - {_lobby.Configuration.MaximumStarRating:.0#}*).");
            }
        }

        HasValidMapPicked = false;
        
        _hostValidMapPicked = false;
        _beatmapRejectTime = DateTime.Now;
        
        _lobby.Bot.RuntimeInfo.Statistics.MapViolations.WithLabels(_lobby.LobbyLabel).Inc();

        EnsureValidMap(false);
        RunViolationAutoSkip();
    }

    private async Task AnnounceNewBeatmap(BeatmapModel beatmapModel, int id)
    {
        try
        {
            // Shouldn't ever be null, but I want to make static analysis happy.
            if (CurrentBeatmap == null || 
                beatmapModel.DifficultyRating == null)
            {
                return;
            }
            
            // By "setting" the map our self directly after the host picked it, 
            // it will automatically be set to the newest version, even if the host's one is outdated.
            SetBeatmap(CurrentBeatmap.Id);

            _lobby.SendMessage($"[https://osu.ppy.sh/b/{id} {beatmapModel.Artist} - {beatmapModel.Title} [{beatmapModel.Version ?? string.Empty}]] - ([https://beatconnect.io/b/{CurrentBeatmap.SetId} BeatConnect Mirror] - [https://osu.direct/d/{CurrentBeatmap.SetId} osu.direct Mirror])");
            
            var starRating = Math.Round(float.Parse(beatmapModel.DifficultyRating, CultureInfo.InvariantCulture), 2);

            if (_lobby.Bot.PerformancePointCalculator == null)
            {
                _lobby.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {CurrentBeatmap.Length:mm\\:ss} | BPM: {beatmapModel.Bpm})");
                _lobby.SendMessage($"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | HP: {beatmapModel.DiffDrain})");
            }
            else
            {
                var ppInfo = await _lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(id);

                _lobby.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {CurrentBeatmap.Length:mm\\:ss} | BPM: {beatmapModel.Bpm})");

                _lobby.SendMessage(ppInfo != null
                    ? $"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | HP: {beatmapModel.DiffDrain} | 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp)"
                    : $"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | HP: {beatmapModel.DiffDrain})");
            }
            
            OnNewAllowedMap?.Invoke();
        }
        catch (Exception)
        {
            // ignored, used to catch weird edge cases within the API
        }
    }
    
    private void SetBeatmap(int id, string? additionalInfo = null)
    {
        _botAppliedBeatmap = true;
        _lastBotAppliedBeatmap = id;
        
        _lobby.SendMessage($"!mp map {id} 0 {additionalInfo ?? string.Empty}");
    }

    private void RunViolationAutoSkip()
    {
        if (_lobby.Configuration.AutomaticallySkipAfterViolations == null ||
            _lobby.Configuration.ViolationSkipCount == null ||
            _autoHostRotateBehaviour == null)
            return;
        
        var skipViolationCount = _lobby.Configuration.ViolationSkipCount.Value;

        _hostViolationCount++;
        
        if (_hostViolationCount < skipViolationCount)
        {
            return;
        }
        
        _hostViolationCount = 0;

        _lobby.SendMessage($"Skipping host automatically due to {skipViolationCount} violations!");
            
        _autoHostRotateBehaviour.SkipCurrentHost();
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

    public enum MapValidation
    {
        None,
        PostStart,
        MapPick
    }
}