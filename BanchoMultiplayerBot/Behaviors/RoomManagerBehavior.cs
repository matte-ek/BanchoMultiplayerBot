using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class RoomManagerBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
    {
        private readonly BehaviorDataProvider<RoomManagerBehaviorData> _dataProvider = new(context.Lobby);
        private RoomManagerBehaviorData Data => _dataProvider.Data;
        
        public async Task SaveData() => await _dataProvider.SaveData();
        
        [BotEvent(BotEventType.Initialize)]
        public async Task OnInitialize()
        {
            if (Data.IsNewInstance)
            {
                await OnMatchFinished();
                return;
            }
            
            await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();
        }

        [BanchoEvent(BanchoEventType.MatchStarted)]
        public async Task OnMatchStarted()
        {
            Data.PlayerFinishCount = 0;
            
            // If there are no players in the lobby, we should abort the match.
            if (context.MultiplayerLobby.Players.Count == 0)
            {
                if (context.Lobby.Health == LobbyHealth.Initializing)
                {
                    // To account for whenever the bot is starting, context.MultiplayerLobby.Players.Count could still be zero,
                    // and therefore it shouldn't automatically abort the match.
                
                    return;
                }
                
                await context.ExecuteCommandAsync<MatchAbortCommand>();
            }
        }

        [BanchoEvent(BanchoEventType.MatchAborted)]
        public async Task OnMatchAborted() => await OnMatchFinished();

        [BanchoEvent(BanchoEventType.MatchFinished)]
        public async Task OnMatchFinished()
        {
            await context.ExecuteCommandAsync<RoomSettingsUpdateCommand>();

            var lobbyConfiguration = await context.Lobby.GetLobbyConfiguration();

            var tasks = new List<Task>
            {
                EnsureRoomName(lobbyConfiguration),
                EnsureRoomPassword(lobbyConfiguration),
                EnsureMatchSettings(lobbyConfiguration),
                EnsureMatchMods(lobbyConfiguration)
            };

            // Yes, awaiting this first is intentional, it does not matter.
            await context.Lobby.BehaviorEventProcessor!.OnBehaviorEvent("RoomManagerMatchFinished");

            await Task.WhenAll(tasks);
        }

        [BanchoEvent(BanchoEventType.MessageReceived)]
        public void OnMessageReceived(IPrivateIrcMessage message)
        {
            if (message.IsBanchoBotMessage == false)
            {
                return;
            }
            
            if (context.MultiplayerLobby.MatchInProgress && message.Content.Contains("finished playing (Score: "))
            {
                Data.PlayerFinishCount++;

                if (Data.PlayerFinishCount > context.MultiplayerLobby.Players.Count / 2)
                {
                    context.TimerProvider.FindOrCreateTimer("PlayerFinishTimer").Start(TimeSpan.FromSeconds(30));
                }
            }
            
            // Handle Bancho restart message for the players
            if (message.Content.StartsWith("Bancho will be restarting for maintenance in 1 minute."))
            {
                // This is hacky
                var spamFilter = new string('\u200B', context.Lobby.LobbyConfigurationId);
                
                context.SendMessage($"Bancho is about to restart, the lobby should be automatically re-created in few minutes after Bancho is restarted. {spamFilter}");
                context.SendMessage($"Try searching for the lobby if you cannot find it in the list, thanks for playing! {spamFilter}");
                
                context.Lobby.Bot.NotificationManager.Notify("Bot", "Bancho will be restarting for maintenance in 1 minute.");
            }
        }

        [BotEvent(BotEventType.TimerElapsed, "PlayerFinishTimer")]
        public async Task OnPlayerFinishTimerElapsed()
        {
            if (!context.MultiplayerLobby.MatchInProgress)
            {
                return;
            }
            
            Log.Warning("RoomManagerBehavior: Detected possibly stuck match, aborting...");
            
            context.SendMessage("Detected possibly stuck match, aborting...");
            
            await context.ExecuteCommandAsync<MatchAbortCommand>();
        }
        
        private async Task EnsureRoomName(LobbyConfiguration configuration)
        {
            if (configuration.Name == context.MultiplayerLobby.Name)
            {
                return;
            }

            await context.ExecuteCommandAsync<RoomSetNameCommand>([configuration.Name]);
        }

        private async Task EnsureRoomPassword(LobbyConfiguration configuration)
        {
            await context.ExecuteCommandAsync<RoomSetPasswordCommand>([configuration.Password ?? ""]);
        }

        private async Task EnsureMatchSettings(LobbyConfiguration configuration)
        {
            var teamMode = ((int)(configuration.TeamMode ?? LobbyFormat.HeadToHead)).ToString();
            var scoreMode = ((int)(configuration.ScoreMode ?? WinCondition.Score)).ToString();
            var size = configuration.Size.ToString() ?? "16";

            await context.ExecuteCommandAsync<MatchSetSettingsCommand>([teamMode, scoreMode, size]);
        }

        private async Task EnsureMatchMods(LobbyConfiguration configuration)
        {
            if (configuration.Mods == null)
            {
                return;
            }

            // No, I can't read this easily either, but it's short. :)
            // Good example of bad code, but it's not worth refactoring.
            Mods desiredMods = configuration.Mods.Aggregate<string, Mods>(0, (current, modName) => current | (Mods)Enum.Parse(typeof(Mods), modName));

            if (context.MultiplayerLobby.Mods == desiredMods)
            {
                return;
            }

            var modsCommandNonSpacing = desiredMods.ToAbbreviatedForm(false);

            if (modsCommandNonSpacing == "None")
            {
                if ((desiredMods & Mods.Freemod) != 0)
                {
                    await context.ExecuteCommandAsync<MatchSetModsCommand>(["Freemod"]);
                }

                return;
            }

            await context.ExecuteCommandAsync<MatchSetModsCommand>([GenerateModsCommand(modsCommandNonSpacing)]);
        }

        private static string GenerateModsCommand(string modsCommandNonSpacing)
        {
            // TODO: Move this madness elsewhere, it probably shouldn't be here.
            // We need to translate the mods command to the format that bancho expects.
            // For example "!mp mods HR HD"

            var modsCommand = "";
            bool newMod = false;

            foreach (var c in modsCommandNonSpacing)
            {
                modsCommand += c;

                if (newMod)
                {
                    modsCommand += ' ';
                    newMod = false;
                    continue;
                }

                newMod = true;
            }

            return modsCommand;
        }
    }
}
