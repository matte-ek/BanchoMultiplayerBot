using System.Globalization;
using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Osu;
using BanchoMultiplayerBot.Osu.Exceptions;
using BanchoMultiplayerBot.Osu.Models;
using BanchoMultiplayerBot.Providers;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class MapManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
    {
        private readonly BehaviorDataProvider<MapManagerBehaviorData> _dataProvider = new(context.Lobby);
        private readonly BehaviorConfigProvider<MapManagerBehaviorConfig> _configProvider = new(context.Lobby);

        private MapManagerBehaviorData Data => _dataProvider.Data;
        private MapManagerBehaviorConfig Config => _configProvider.Data;

        private OsuApi OsuApi => context.Lobby.Bot.OsuApi;

        public async Task SaveData() => await _dataProvider.SaveData();

        [BanchoEvent(BanchoEventType.MatchStarted)]
        public async Task OnMatchStarted()
        {
            await ValidatePlayingMap();
        }
            
        [BanchoEvent(BanchoEventType.MatchFinished)]
        public async Task OnMatchFinished()
        {
            var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
            var channelId = context.Lobby.BanchoConnection.ChannelHandler.GetChannelId(context.MultiplayerLobby.ChannelName) ?? 0;
            
            foreach (var player in Data.MatchFinishMessageSubscribers)
            {
                context.Lobby.BanchoConnection.MessageHandler.SendMessage(player, $"Match has finished in the [osu://mp/{channelId} {lobbyConfig.Name}] lobby!");
            }

            Data.MatchFinishMessageSubscribers.Clear();
        }

        [BanchoEvent(BanchoEventType.OnHostChanged)]
        public void OnHostChanged()
        {
            // psst, this allows the violation count to be bypassed by passing
            // the host to someone else, after which the bot will give back the host to the original host.
            Data.HostViolationCount = 0;
        }

        [BanchoEvent(BanchoEventType.OnMapChanged)]
        public async Task OnMapChanged(BeatmapShell beatmapShell)
        {
            // If the beatmap is the same as the last one applied, we don't need to do anything.
            // This is to prevent the bot from applying the same beatmap multiple times.
            if (beatmapShell.Id == Data.LastBotAppliedBeatmapId)
            {
                return;
            }

            Data.LastPlayerAppliedBeatmapId = beatmapShell.Id;

            try
            {
                var beatmapInfo = await OsuApi.GetBeatmapInformation(beatmapShell.Id);
                if (beatmapInfo == null)
                {
                    Log.Error("MapManagerBehavior: Failed to get beatmap information for map {BeatmapId}", beatmapShell.Id);
                    
                    context.SendMessage("osu!api error while trying to get beatmap information");

                    return;
                }

                var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
                var mapValidator = new MapValidator(lobbyConfig, Config);
                var mapValidationResult = await mapValidator.ValidateBeatmap(beatmapInfo);

                // Special case for double-time, we want to check if
                // the map was rejected for star rating
                if (mapValidationResult == MapValidator.MapStatus.StarRating &&
                    beatmapInfo.DifficultyRating != null &&
                    float.TryParse(beatmapInfo.DifficultyRating, NumberStyles.Float, new CultureInfo("en-US"), out var diffRating) &&
                    Config.MinimumStarRating >= Math.Round(diffRating, 2) &&
                    Config.AllowDoubleTime)
                {
                    var doubleTimeMapInfo = await OsuApi.GetBeatmapInformation(beatmapShell.Id, ModsModel.DoubleTime);
                 
                    // Check if map is OK with double time
                    if (doubleTimeMapInfo != null &&
                        await mapValidator.ValidateBeatmap(doubleTimeMapInfo) == MapValidator.MapStatus.Ok)
                    {
                        // Check room settings to make sure double time is enabled
                        await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();

                        if ((context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0)
                        {
                            await EnforceBeatmapRegulations(doubleTimeMapInfo, MapValidator.MapStatus.Ok, ModsModel.DoubleTime);

                            return;
                        }
                    }
                }

                await EnforceBeatmapRegulations(beatmapInfo, mapValidationResult);
            }
            catch (BeatmapNotFoundException)
            {
                Log.Information("MapManagerBehavior: Beatmap {BeatmapId} not found, applying fallback beatmap", beatmapShell.Id);
                
                await ApplyBeatmap(Data.BeatmapFallbackId);
                
                context.SendMessage("The selected beatmap is not submitted, please pick another one");
            }
            catch (HttpRequestException e)
            {
                Log.Error("MapManagerBehavior: Timed out getting beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);
                
                context.SendMessage("osu!api timed out while trying to get beatmap information");
            }
            catch (InvalidApiKeyException)
            {
                Log.Error("MapManagerBehavior: Invalid API key while trying to get beatmap information for map {BeatmapId}", beatmapShell.Id);

                context.SendMessage("Internal error while trying to get beatmap information");
            }
            catch (Exception e)
            {
                Log.Error(e, "MapManagerBehavior: Exception while trying to get beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);
                
                context.SendMessage("Internal error while trying to get beatmap information");
            }
        }

        private async Task EnforceBeatmapRegulations(BeatmapModel beatmapModel, MapValidator.MapStatus status, ModsModel mods = 0)
        {
            var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
            
            Log.Verbose("MapManagerBehavior: Enforcing beatmap regulations for map {BeatmapId}, status: {MapStatus}", beatmapModel.BeatmapId, status);

            if (status == MapValidator.MapStatus.Ok)
            {
                Data.BeatmapInfo = new BeatmapInfo()
                {
                    Id = int.Parse(beatmapModel.BeatmapId ?? "0"),
                    SetId = int.Parse(beatmapModel.BeatmapsetId ?? "0"),
                    Name = $"{beatmapModel.Artist} - {beatmapModel.Title}",
                    Length = TimeSpan.FromSeconds(int.Parse(beatmapModel.TotalLength ?? "0", CultureInfo.InvariantCulture)),
                    DrainLength = TimeSpan.FromSeconds(int.Parse(beatmapModel.HitLength ?? "0", CultureInfo.InvariantCulture)),
                    StarRating = float.Parse(beatmapModel.DifficultyRating ?? "0", CultureInfo.InvariantCulture)
                };

                Data.BeatmapFallbackId = Data.BeatmapInfo.Id;
                
                // By "setting" the map our self directly after the host picked it, 
                // it will automatically be set to the newest version, even if the host's one is outdated.
                await ApplyBeatmap(Data.BeatmapInfo.Id);
                await AnnounceBeatmap(beatmapModel, mods);

                // Fire off any "new map" events
                await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("MapManagerNewMap");
                
                return;
            }

            await ApplyBeatmap(Data.BeatmapFallbackId);

            switch (status)
            {
                case MapValidator.MapStatus.Length:
                    var configuredMaxMapLength = TimeSpan.FromSeconds(Config.MaximumMapLength);
                    context.SendMessage($"The selected beatmap you've picked is too long. Max map length: {configuredMaxMapLength:mm\\:ss}");
                    break;
                case MapValidator.MapStatus.StarRating:
                    var mapStarRating = Math.Round(float.Parse(beatmapModel.DifficultyRating!, CultureInfo.InvariantCulture), 2);

                    context.SendMessage(mapStarRating >= Config.MaximumStarRating
                        ? $"The selected beatmap's star rating is too high for the lobby ({mapStarRating:0.00} > {Config.MaximumStarRating:0.0})."
                        : $"The selected beatmap's star rating is too low for the lobby ({Config.MinimumStarRating:0.0} > {mapStarRating:0.00}).");
                    break;
                case MapValidator.MapStatus.GameMode:
                    var modeName = lobbyConfig.Mode switch
                    {
                        GameMode.osu => "osu!std",
                        GameMode.osuCatch => "osu!catch",
                        GameMode.osuMania => "osu!mania",
                        GameMode.osuTaiko => "osu!taiko",
                        _ => "Error"
                    };

                    context.SendMessage($"Please only pick beatmaps from the game mode {modeName}.");
                    break;
                case MapValidator.MapStatus.Banned:
                    context.SendMessage(beatmapModel.Title != null
                        ? $"The selected beatmap ({beatmapModel.Title}) is not allowed."
                        : "The selected beatmap is not allowed.");
                    break;
                case MapValidator.MapStatus.Removed:
                    context.SendMessage("The selected beatmap has been removed from the osu! website");
                    break;
            }

            await HandleViolationAutoSkip();
        }

        [BotEvent(BotEventType.CommandExecuted, "Regulations")]
        public async Task OnRegulationsCommandExecuted(CommandEventContext commandEventContext)
        {
            var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
            var timeSpan = TimeSpan.FromSeconds(Config.MaximumMapLength);

            var requiredModeName = lobbyConfig.Mode switch
            {
                GameMode.osu => "osu!std",
                GameMode.osuCatch => "osu!catch",
                GameMode.osuMania => "osu!mania",
                GameMode.osuTaiko => "osu!taiko",
                _ => "Any Mode"
            };

            commandEventContext.Reply($"Star rating: {Config.MinimumStarRating:.0#}* - {Config.MaximumStarRating:.0#}* | Max length: {timeSpan:mm\\:ss} | {requiredModeName}");
        }

        [BotEvent(BotEventType.CommandExecuted, "Mirror")]
        public void OnMirrorCommandExecuted(CommandEventContext commandEventContext)
        {
            commandEventContext.Reply($"[https://beatconnect.io/b/{Data.BeatmapInfo.SetId} BeatConnect Mirror] - [https://osu.direct/d/{Data.BeatmapInfo.SetId} osu.direct Mirror]");
        }
        
        [BotEvent(BotEventType.CommandExecuted, "TimeLeft")]
        public async Task OnTimeLeftCommandExecuted(CommandEventContext commandEventContext)
        {
            var finishTime = Data.MatchStartTime.Add(Data.BeatmapInfo.Length);
            
            // Add a few seconds to account for people loading/finishing the map
            finishTime = finishTime.AddSeconds(5);
            
            // If we know where the map actually starts (as in the break at the beginning of a song),
            // we can be a bit more clever and try to account for that.
            // This obviously assumes that the players skip, and skip at around 50% of that time.
            var beginSkipTime = await BeatmapProcessor.GetBeatmapStartTime(Data.BeatmapInfo.Id);
            if (beginSkipTime != null)
            {
                finishTime = finishTime.Subtract(TimeSpan.FromSeconds(beginSkipTime.Value * 0.5));
            }
            
            var timeLeft = (finishTime - DateTime.UtcNow).ToString(@"mm\:ss");
            
            // Allow the players to get pinged when the map is finished
            var pingEnabled = commandEventContext.Arguments.Length > 0 && commandEventContext.Arguments[0].Equals("ping", StringComparison.OrdinalIgnoreCase) && commandEventContext.Player != null;
            if (pingEnabled)
            {
                Data.MatchFinishMessageSubscribers.Add(commandEventContext.Player!.Name);
            }
            
            commandEventContext.Reply(pingEnabled
                ? $"Estimated time left of current map: {timeLeft}, you will be notified when the map is finished."
                : $"Estimated time left of current map: {timeLeft}");
        }

        /// <summary>
        /// Will announce the beatmap in the chat, along with some information about it,
        /// such as star rating, status, length, bpm, etc.
        /// </summary>
        private async Task AnnounceBeatmap(BeatmapModel beatmapModel, ModsModel mods = 0)
        {
            var beatmapInfo = Data.BeatmapInfo;
            var starRating = Math.Round(float.Parse(beatmapModel.DifficultyRating ?? "0", CultureInfo.InvariantCulture), 2);

            context.SendMessage($"[https://osu.ppy.sh/b/{beatmapInfo.Id} {beatmapModel.Artist} - {beatmapModel.Title} [{beatmapModel.Version ?? string.Empty}]] - ([https://beatconnect.io/b/{beatmapInfo.SetId} BeatConnect Mirror] - [https://osu.direct/d/{beatmapInfo.SetId} osu.direct Mirror])");
            context.SendMessage($"(Star Rating: {starRating:.0#} | {beatmapModel.GetStatusString()} | Length: {beatmapInfo.Length:mm\\:ss} | BPM: {beatmapModel.Bpm})");

            // If the bot has a performance point calculator, we can calculate the performance points for the beatmap.
            if (PerformancePointCalculator.IsAvailable && 
                context.Lobby.Bot.PerformancePointCalculator != null)
            {
                var ppInfo = await context.Lobby.Bot.PerformancePointCalculator.CalculatePerformancePoints(beatmapInfo.Id, (int)mods);
                if (ppInfo != null)
                {
                    context.SendMessage($"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | HP: {beatmapModel.DiffDrain} | 100%: {ppInfo.Performance100}pp | 98%: {ppInfo.Performance98}pp | 95%: {ppInfo.Performance95}pp)");

                    return;
                }
            }
         
            context.SendMessage($"(AR: {beatmapModel.DiffApproach} | CS: {beatmapModel.DiffSize} | OD: {beatmapModel.DiffOverall} | HP: {beatmapModel.DiffDrain})");
        }

        /// <summary>
        /// Will automatically skip the host if their violation count exceeds the maximum amount of violations.
        /// </summary>
        private async Task HandleViolationAutoSkip()
        {
            if (Config.AutomaticallySkipHostViolations == false ||
                Config.MaximumHostViolations == 0)
            {
                return;
            }

            Data.HostViolationCount++;

            if (Data.HostViolationCount < Config.MaximumHostViolations)
            {
                return;
            }

            Data.HostViolationCount = 0;

            context.SendMessage($"Skipping host automatically due to {Config.MaximumHostViolations} violations!");
            
            await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("HostQueueSkipHost");
        }

        /// <summary>
        /// Applies the provided beatmap id to the lobby, will also set the last bot applied beatmap id,
        /// so we don't end up in a loop of applying the same beatmap over and over.
        /// </summary>
        private async Task ApplyBeatmap(int beatmapId)
        {
            Data.LastBotAppliedBeatmapId = beatmapId;
            await context.ExecuteCommandAsync<MatchSetBeatmapCommand>([beatmapId.ToString(), "0"]);
        }

        /// <summary>
        /// This method will validate the map after we've just started the match.
        /// We do this to avoid any host trying to cheat the system, or to avoid any
        /// stupid double-time picks.
        /// </summary>
        private async Task ValidatePlayingMap()
        {
            await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();

            try
            {
                // osu!api has different bits for each mod, so we need to "translate" it.
                // We only really care about the difficulty increasing mods anyway.
                ModsModel osuApiMods = 0;

                if ((context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0 ||
                    (context.MultiplayerLobby.Mods & Mods.Nightcore) != 0)
                    osuApiMods |= ModsModel.DoubleTime;
                if ((context.MultiplayerLobby.Mods & Mods.HardRock) != 0)
                    osuApiMods |= ModsModel.HardRock;

                var mapIsValid = false;

                if (!Config.AllowDoubleTime && (osuApiMods & ModsModel.DoubleTime) != 0)
                {
                    mapIsValid = false;
                }

                var beatmapInfo = await OsuApi.GetBeatmapInformation(Data.LastPlayerAppliedBeatmapId, osuApiMods);
                if (beatmapInfo != null)
                {
                    var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
                    var mapValidator = new MapValidator(lobbyConfig, Config);
                    var mapValidationResult = await mapValidator.ValidateBeatmap(beatmapInfo);

                    if (mapValidationResult != MapValidator.MapStatus.Ok)
                    {
                        mapIsValid = false;
                    }
                }

                if (mapIsValid)
                {
                    // Everything checks out, we're done
                    return;
                }
                
                Log.Error("MapManagerBehavior: Detected an attempt to play a map out of the lobby's star rating! Aborting...");
                context.SendMessage("Detected an attempt to play a map out of the lobby's star rating! Aborting...");

                await context.ExecuteCommandAsync<MatchAbortCommand>();
            }
            catch (Exception e)
            {
                Log.Error(e, "MapManagerBehavior: Exception while trying to validate the map, {e}", e);
            }
        }
    }
}