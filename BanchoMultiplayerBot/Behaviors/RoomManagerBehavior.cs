using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Behaviors
{
    public class RoomManagerBehavior(BotEventContext context) : IBehavior
    {
        [BanchoEvent(BanchoEventType.MatchStarted)]
        public async Task OnMatchStarted()
        {
            if (context.MultiplayerLobby.Players.Count == 0)
            {
                await context.ExecuteCommandAsync<MatchAbortCommand>();
            }
        }

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
            if (configuration.Size == null)
            {
                return;
            }

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

            await context.ExecuteCommandAsync<MatchSetModsCommand>([modsCommand]);
        }
    }
}
