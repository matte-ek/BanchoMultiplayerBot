using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Behaviors.Config;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Osu;
using BanchoMultiplayerBot.Providers;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Multiplayer;
using osu.NET;
using osu.NET.Enums;
using osu.NET.Models.Beatmaps;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class MapManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
    {
        private readonly BehaviorDataProvider<MapManagerBehaviorData> _dataProvider = new(context.Lobby);
        private readonly BehaviorConfigProvider<MapManagerBehaviorConfig> _configProvider = new(context.Lobby);

        private MapManagerBehaviorData Data => _dataProvider.Data;
        private MapManagerBehaviorConfig Config => _configProvider.Data;
        
        public async Task SaveData() => await _dataProvider.SaveData();

        [BanchoEvent(BanchoEventType.MatchStarted)]
        public async Task OnMatchStarted()
        {
            Data.MatchStartTime = DateTime.UtcNow;
            
            await ValidatePlayingMap();
        }
            
        [BanchoEvent(BanchoEventType.MatchFinished)]
        public async Task OnMatchFinished()
        {
            var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
            var channelId = context.Lobby.BanchoConnection.ChannelHandler.GetChannelRuntimeId(context.MultiplayerLobby.ChannelName) ?? 0;
            
            foreach (var player in Data.MatchFinishMessageSubscribers)
            {
                context.Lobby.BanchoConnection.MessageHandler.SendMessage(player, $"Match has finished in the [osu://mp/{channelId} {lobbyConfig.Name}] lobby!");
            }

            Data.MatchFinishMessageSubscribers.Clear();
        }

        [BanchoEvent(BanchoEventType.HostChanged)]
        public void OnHostChanged()
        {
            // psst, this allows the violation count to be bypassed by passing
            // the host to someone else, after which the bot will give back the host to the original host.
            Data.HostViolationCount = 0;
        }

        [BanchoEvent(BanchoEventType.MapChanged)]
        public async Task OnMapChanged(BeatmapShell beatmapShell)
        {
            Data.CurrentMapId = beatmapShell.Id;
            
            // If the beatmap is the same as the last one applied, we don't need to do anything.
            // This is to prevent the bot from applying the same beatmap multiple times.
            if (beatmapShell.Id == Data.LastBotAppliedBeatmapId)
            {
                return;
            }

            Data.LastPlayerAppliedBeatmapId = beatmapShell.Id;
            
            try
            {
                var beatmapInfoResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetBeatmapAsync(beatmapShell.Id));
                var beatmapAttributesResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(beatmapShell.Id));
                
                if (beatmapInfoResult.IsFailure)
                {
                    if (beatmapInfoResult.Error.Type == ApiErrorType.BeatmapNotFound)
                    {
                        Log.Information("MapManagerBehavior: Beatmap {BeatmapId} not found, applying fallback beatmap", beatmapShell.Id);
                
                        await ApplyBeatmap(Data.BeatmapFallbackId);
                
                        context.SendMessage("The selected beatmap is not submitted, please pick another one");

                        return;
                    }

                    // Not sure what we want to do in this scenario, we'll just have to trust the 
                    // players pick valid maps until the API gets into a better state.
                    
                    Log.Error("MapManagerBehavior: Failed to get beatmap information for map {BeatmapId}", beatmapShell.Id);
                    
                    context.SendMessage("osu!api error while trying to get beatmap information");
                    
                    return;
                }
                
                if (beatmapAttributesResult.IsFailure)
                {
                    Log.Error("MapManagerBehavior: Failed to get beatmap information for map {BeatmapId}", beatmapShell.Id);
                    
                    context.SendMessage("osu!api error while trying to get beatmap information");

                    return;
                }
                
                var beatmapInfo =  beatmapInfoResult.Value!;
                var beatmapAttributes = beatmapAttributesResult.Value!;
                var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
                var mapValidator = new MapValidator(context.Lobby, lobbyConfig, Config);
                var mapValidationResult = await mapValidator.ValidateBeatmap(beatmapAttributes, beatmapInfo);

                // Special case for double-time, we want to check if
                // the map was rejected for star rating
                if (mapValidationResult == MapValidator.MapStatus.StarRating &&
                    Config.MinimumStarRating >= Math.Round(beatmapAttributes.DifficultyRating, 2) &&
                    Config.AllowDoubleTime)
                {
                    var doubleTimeMapAttributesResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(beatmapShell.Id, Ruleset.Osu, ["DT"]));
                 
                    // Check if map is OK with double time
                    if (doubleTimeMapAttributesResult.IsSuccess &&
                        await mapValidator.ValidateBeatmap(doubleTimeMapAttributesResult.Value!, null) == MapValidator.MapStatus.Ok)
                    {
                        // Check room settings to make sure double time is enabled
                        await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();

                        if ((context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0)
                        {
                            await EnforceBeatmapRegulations(beatmapInfo, doubleTimeMapAttributesResult.Value!, MapValidator.MapStatus.Ok, (int)Mods.DoubleTime);

                            return;
                        }
                    }
                }

                await EnforceBeatmapRegulations(beatmapInfo, beatmapAttributes, mapValidationResult);
            }
            catch (HttpRequestException e)
            {
                Log.Error("MapManagerBehavior: Timed out getting beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);
                
                context.SendMessage("osu!api timed out while trying to get beatmap information");
            }
            catch (Exception e)
            {
                Log.Error(e, "MapManagerBehavior: Exception while trying to get beatmap information for map {BeatmapId}, {e}", beatmapShell.Id, e);
                
                context.SendMessage("Internal error while trying to get beatmap information");
            }
        }

        private async Task EnforceBeatmapRegulations(BeatmapExtended beatmapModel, DifficultyAttributes difficultyAttributes, MapValidator.MapStatus status, int mods = 0)
        {
            var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
            var beatmapSet = beatmapModel.Set;

            Log.Verbose("MapManagerBehavior: Enforcing beatmap regulations for map {BeatmapId}, status: {MapStatus}", beatmapModel.Id, status);

            if (status == MapValidator.MapStatus.Ok)
            {
                Data.BeatmapInfo = new BeatmapInfo
                {
                    Id = beatmapModel.Id,
                    SetId = beatmapModel.SetId,
                    Name = beatmapSet?.Title ?? string.Empty,
                    Artist = beatmapSet?.Artist ?? string.Empty,
                    Length = beatmapModel.TotalLength,
                    DrainLength = beatmapModel.HitLength,
                    StarRating = difficultyAttributes.DifficultyRating
                };

                Data.BeatmapFallbackId = Data.BeatmapInfo.Id;
                
                // By "setting" the map our self directly after the host picked it, 
                // it will automatically be set to the newest version, even if the host's one is outdated.
                await ApplyBeatmap(Data.BeatmapInfo.Id);
                await AnnounceBeatmap(beatmapModel, difficultyAttributes, mods);

                // Fire off any "new map" events
                await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("MapManagerNewMap");
                
                return;
            }

            await ApplyBeatmap(Data.BeatmapFallbackId);

            switch (status)
            {
                case MapValidator.MapStatus.Length:
                    context.SendMessage(beatmapModel.TotalLength.TotalSeconds >= Config.MaximumMapLength
                        ? $"The selected beatmap you've picked is too long. Max map length: {TimeSpan.FromSeconds(Config.MaximumMapLength):mm\\:ss}"
                        : $"The selected beatmap you've picked is too short. Min map length: {TimeSpan.FromSeconds(Config.MinimumMapLength):mm\\:ss}");
                    
                    break;
                case MapValidator.MapStatus.StarRating:
                    context.SendMessage(difficultyAttributes.DifficultyRating >= Config.MaximumStarRating
                        ? $"The selected beatmap's star rating is too high for the lobby ({difficultyAttributes.DifficultyRating:0.00} > {Config.MaximumStarRating:0.0})."
                        : $"The selected beatmap's star rating is too low for the lobby ({Config.MinimumStarRating:0.0} > {difficultyAttributes.DifficultyRating:0.00}).");
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
                    context.SendMessage(beatmapSet?.Title != null
                        ? $"The selected beatmap ({beatmapSet.Title}) is not allowed."
                        : "The selected beatmap is not allowed.");
                    break;
                case MapValidator.MapStatus.Removed:
                    context.SendMessage("The selected beatmap has been removed from the osu! website");
                    break;
            }

            await HandleViolationAutoSkip();
            await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("MapManagerInvalidMap");
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
            commandEventContext.Reply($"[https://beatconnect.io/b/{Data.BeatmapInfo.SetId} BeatConnect Mirror] - [https://osu.direct/d/{Data.BeatmapInfo.SetId} osu.direct Mirror] - [https://catboy.best/d/{Data.BeatmapInfo.SetId} Mino Mirror]");
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

            if (DateTime.UtcNow >= finishTime)
            {
                timeLeft = "00:00";
            }
            
            // Allow the players to get pinged when the map is finished
            var pingEnabled = commandEventContext.Arguments.Length > 0 && commandEventContext.Arguments[0].Equals("ping", StringComparison.OrdinalIgnoreCase) && commandEventContext.Player != null;
            if (pingEnabled)
            {
                Data.MatchFinishMessageSubscribers.Add(commandEventContext.Player!.Name.ToIrcNameFormat());
            }
            
            commandEventContext.Reply(pingEnabled
                ? $"Estimated time left of current map: {timeLeft}, you will be notified when the map is finished."
                : $"Estimated time left of current map: {timeLeft}");
        }

        /// <summary>
        /// Will announce the beatmap in the chat, along with some information about it,
        /// such as star rating, status, length, bpm, etc.
        /// </summary>
        private async Task AnnounceBeatmap(BeatmapExtended beatmapModel, DifficultyAttributes difficultyAttributes, int mods)
        {
            var beatmapInfo = Data.BeatmapInfo;
            var starRatingRounded = Math.Round(difficultyAttributes.DifficultyRating, 2);
            var beatmapSet = beatmapModel.Set;
            
            context.SendMessage($"[https://osu.ppy.sh/b/{beatmapInfo.Id} {beatmapSet?.Artist} - {beatmapSet?.Title} [{beatmapModel.Version}]] - ([https://beatconnect.io/b/{beatmapInfo.SetId} BeatConnect Mirror] - [https://osu.direct/d/{beatmapInfo.SetId} osu.direct Mirror] - [https://catboy.best/d/{beatmapInfo.SetId} Mino Mirror])");
            context.SendMessage($"(Star Rating: {starRatingRounded:.0#} | {beatmapModel.Status.ToString()} | Length: {beatmapInfo.Length:mm\\:ss} | BPM: {beatmapModel.BPM})");

            // If the bot has a performance point calculator, we can calculate the performance points for the beatmap.
            if (context.Lobby.Bot.PerformancePointService is { IsAvailable: true })
            {
                var ppInfo = await context.Lobby.Bot.PerformancePointService.CalculatePerformancePoints(beatmapInfo.Id, mods, beatmapModel.LastUpdated);
                if (ppInfo != null)
                {
                    context.SendMessage($"(AR: {beatmapModel.ApproachRate} | CS: {beatmapModel.CircleSize} | OD: {beatmapModel.OverallDifficulty} | HP: {beatmapModel.HealthDrain} | 100%: {(int)ppInfo.Performance100}pp | 98%: {(int)ppInfo.Performance98}pp | 95%: {(int)ppInfo.Performance95}pp)");

                    return;
                }
            }
         
            context.SendMessage($"(AR: {beatmapModel.ApproachRate} | CS: {beatmapModel.CircleSize} | OD: {beatmapModel.OverallDifficulty} | HP: {beatmapModel.HealthDrain})");
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
            if (Data.CurrentMapId == 0)
            {
                return;
            }
            
            await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();

            bool mapIsValid = !(!Config.AllowDoubleTime && (context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0);

            List<string> mods = [];

            // We do this to only add the difficulty increasing mods, we don't really 
            // care about the rest, and therefore don't need to send it to the API.
                
            if ((context.MultiplayerLobby.Mods & Mods.DoubleTime) != 0 || 
                (context.MultiplayerLobby.Mods & Mods.Nightcore) != 0)
                mods.Add("DT");
            if ((context.MultiplayerLobby.Mods & Mods.HardRock) != 0)
                mods.Add("HR");
            if ((context.MultiplayerLobby.Mods & Mods.Hidden) != 0)
                mods.Add("HD");
                
            var difficultyAttributesResult = await context.UsingApiClient(async (apiClient) => await apiClient.GetDifficultyAttributesAsync(Data.CurrentMapId, Ruleset.Osu, mods.ToArray()));
            if (difficultyAttributesResult.IsSuccess)
            {
                var lobbyConfig = await context.Lobby.GetLobbyConfiguration();
                var mapValidator = new MapValidator(context.Lobby, lobbyConfig, Config);
                var mapValidationResult = await mapValidator.ValidateBeatmap(difficultyAttributesResult.Value!, null);

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
    }
}
