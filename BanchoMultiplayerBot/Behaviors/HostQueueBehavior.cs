using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Behaviors.Data;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class HostQueueBehavior(BehaviorEventContext context) : IBehavior, IBehaviorDataConsumer
    {
        private readonly BehaviorDataProvider<HostQueueBehaviorData> _dataProvider = new(context.Lobby);
        private HostQueueBehaviorData Data => _dataProvider.Data;
        
        public async Task SaveData() => await _dataProvider.SaveData();

        [BotEvent(BotEventType.BehaviourEvent, "HostQueueSkipHost")]
        public async Task OnSkipRequested() => await SkipHost();
        
        [BanchoEvent(BanchoEventType.OnSettingsUpdated)]
        public async Task OnSettingsUpdated()
        {
            // Since we might be picking up where we left off, we need to ensure the queue is valid
            // since some players might have left while the bot was offline
            await EnsureQueueValid();
        }

        [BanchoEvent(BanchoEventType.OnPlayerJoined)]
        public async Task OnPlayerJoined(MultiplayerPlayer player)
        {
            using var userRepository = new UserRepository();
            var user = await userRepository.FindOrCreateUser(player.Name);

            if (user.Bans.Count != 0)
            {
                Log.Warning("HostQueueBehavior: Player {PlayerName} is banned, skipping queue", player.Name);
                return;
            }
            
            if (Data.Queue.Contains(player.Name))
            {
                Log.Verbose("HostQueueBehavior: Player {PlayerName} is already in the queue during join event", player.Name);
                return;
            }
            
            Data.Queue.Add(player.Name);
            
            RestoreQueuePosition(player);
            
            if (context.Lobby.Health != LobbyHealth.Initializing)
                ApplyRoomHost();
        }
        
        [BanchoEvent(BanchoEventType.OnPlayerDisconnected)]
        public void OnPlayerDisconnected(MultiplayerPlayer player)
        {
            if (!Data.Queue.Contains(player.Name))
            {
                Log.Warning("HostQueueBehavior: Player {PlayerName} is not in the queue during disconnect event", player.Name);
                return;
            }
            
            StoreQueuePosition(player);
            
            Data.Queue.Remove(player.Name);
            
            ApplyRoomHost();
        }

        [BotEvent(BotEventType.BehaviourEvent, "RoomManagerMatchFinished")]
        public async Task OnManagerMatchFinished()
        {
            await SkipHost();
            
            context.SendMessage(GetCurrentQueueMessage(true));
        }
        
        [BotEvent(BotEventType.CommandExecuted, "Queue")]
        public void OnQueueCommandExecuted(CommandEventContext commandEventContext)
        {
            commandEventContext.Reply(GetCurrentQueueMessage());
        }
        
        [BotEvent(BotEventType.CommandExecuted, "QueuePosition")]
        public void OnQueuePositionCommandExecuted(CommandEventContext commandEventContext)
        {
            if (commandEventContext.Player == null)
            {
                return;
            }

            var queuePosition = Data.Queue.FindIndex(x => x.ToIrcNameFormat().Equals(commandEventContext.Player.Name.ToIrcNameFormat()));

            commandEventContext.Reply(queuePosition == -1
                ? "Couldn't find player in queue."
                : $"Queue position for {commandEventContext.Message.Sender}: #{(queuePosition + 1).ToString()}");
        }
        
        [BotEvent(BotEventType.CommandExecuted, "Skip")]
        public async Task OnSkipCommandExecuted(CommandEventContext commandEventContext)
        {
            if (commandEventContext.Player == null)
            {
                return;
            }

            // If the player requesting is a host, skip immediately
            if (commandEventContext.Player.Name.ToIrcNameFormat() == Data.Queue.FirstOrDefault()?.ToIrcNameFormat())
            {
                await SkipHost();

                return;
            }

            // Else initiate a vote
            var skipVote = context.Lobby.VoteProvider!.FindOrCreateVote("SkipVote", "Skip the host");
            if (skipVote.PlayerVote(commandEventContext.Player))
            {
                await SkipHost();
            }
        }

        [BotEvent(BotEventType.CommandExecuted, "AutoSkip")]
        public async Task OnAutoSkipCommandExecuted(CommandEventContext commandEventContext)
        {
            using var userRepository = new UserRepository();

            if (commandEventContext.Player == null)
            {
                return;
            }
            
            var user = await userRepository.FindOrCreateUser(commandEventContext.Player.Name);
            
            if (commandEventContext.Arguments.Length == 0)
            {
                commandEventContext.Reply($"{commandEventContext.Player.Name}, your auto-skip is currently {(user.AutoSkipEnabled ? "enabled" : "disabled")}");
                return;
            }

            var autoSkipEnabled = commandEventContext.Arguments[0].ToLower() == "on" ||
                                  commandEventContext.Arguments[0].ToLower() == "true" ||
                                  commandEventContext.Arguments[0].ToLower() == "yes";
            
            user.AutoSkipEnabled = autoSkipEnabled;
            
            commandEventContext.Reply($"{commandEventContext.Player.Name}, your auto-skip is now {(user.AutoSkipEnabled ? "enabled" : "disabled")}");
            
            await userRepository.Save();
        }

        [BotEvent(BotEventType.CommandExecuted, "ForceSkip")]
        public async Task OnForceSkipCommandExecuted(CommandEventContext commandEventContext)
        {
            await SkipHost();
        }
        
        [BotEvent(BotEventType.CommandExecuted, "SetHost")]
        public void OnSetHostCommandExecuted(CommandEventContext commandEventContext)
        {
            var player = context.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == commandEventContext.Arguments[0].ToIrcNameFormat());

            if (player == null)
            {
                commandEventContext.Reply("Player not found in lobby.");
                return;
            }

            Data.Queue.RemoveAll(x => x == player.Name);
            Data.Queue.Insert(0, player.Name);
            
            ApplyRoomHost();
        }
        
        [BotEvent(BotEventType.CommandExecuted, "SetQueuePosition")]
        public void OnSetQueuePositionCommandExecuted(CommandEventContext commandEventContext)
        {
            var player = context.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == commandEventContext.Arguments[0].ToIrcNameFormat());

            if (player == null)
            {
                commandEventContext.Reply("Player not found in lobby.");
                return;
            }
            
            if (!int.TryParse(commandEventContext.Arguments[1], out int position))
            {
                commandEventContext.Reply("Invalid position argument.");
                return;
            }
            
            if (position < 0 || position > Data.Queue.Count)
            {
                commandEventContext.Reply("Queue position out of bounds.");
                return;
            }
            
            Data.Queue.RemoveAll(x => x == player.Name);
            Data.Queue.Insert(position, player.Name);
        }

        /// <summary>
        /// Send the first 5 people in the queue in the lobby chat. The player names will include a 
        /// zero width space to avoid tagging people, however this can be ignored for the host
        /// via the tagHost argument.
        /// </summary>
        private string GetCurrentQueueMessage(bool tagHost = false)
        {
            var queueStr = "";
            var cleanPlayerNamesQueue = new List<string>();

            // Add a zero width space to the player names to avoid mentioning them
            Data.Queue.ForEach(playerName => cleanPlayerNamesQueue.Add($"{playerName[0]}\u200B{playerName[1..]}"));

            // Replace the host with the original name, if requested.
            if (tagHost && cleanPlayerNamesQueue.Count != 0)
            {
                cleanPlayerNamesQueue.RemoveAt(0);
                cleanPlayerNamesQueue.Insert(0, Data.Queue.First());
            }

            // Compile a queue string that is shorter than 100 characters.
            foreach (var name in cleanPlayerNamesQueue)
            {
                if (queueStr.Length > 100)
                {
                    queueStr = queueStr[..^2] + "...";
                    break;
                }

                queueStr += name + (name != cleanPlayerNamesQueue.Last() ? ", " : string.Empty);
            }

            return $"Queue: {queueStr}";
        }
        
        /// <summary>
        /// Will make sure the host is set to the first player in the queue, if it isn't already.
        /// Call this method whenever the queue is modified.
        /// </summary>
        private void ApplyRoomHost()
        {
            if (Data.Queue.Count == 0)
            {
                return;
            }
            
            if (context.MultiplayerLobby.Host != null && 
                Data.Queue.First().ToIrcNameFormat() == context.MultiplayerLobby.Host.Name.ToIrcNameFormat())
            {
                // Correct host is already set, no need to do anything
                return;
            }
            
            // Set the host to the first player in the queue
            context.SendMessage($"!mp host {context.GetPlayerIdentifier(Data.Queue.First())}");
        }
        
        /// <summary>
        /// Will skip the current host to the next valid player in the queue.
        /// </summary>
        private async Task SkipHost()
        {
            if (Data.Queue.Count == 0)
            {
                // No players in the queue, nothing to do
                return;
            }
        
            context.Lobby.VoteProvider?.FindOrCreateVote("SkipVote", "Skip the host").Abort();
            
            RotateQueue();
            
            foreach (var player in Data.Queue)
            {
                // Make sure the new host is a valid candidate,
                // if not, skip to the next player in the queue
                if (!await IsPlayerHostCandidate(player))
                {
                    RotateQueue();
                }
            }
            
            ApplyRoomHost();
        }
        
        /// <summary>
        /// Will rotate the queue by moving the first player to the end of the queue,
        /// does NOT apply the new host to the room.
        /// </summary>
        private void RotateQueue()
        {
            if (Data.Queue.Count == 0)
            {
                return;
            }

            var currentHost = Data.Queue.First();
            
            Data.Queue.RemoveAt(0);
            Data.Queue.Add(currentHost);
        }

        private void StoreQueuePosition(MultiplayerPlayer player)
        {
            // Make sure we won't end up with duplicate records
            if (Data.PreviousQueueRecords.Any(x => x.Name == player.Name))
            {
                Data.PreviousQueueRecords.RemoveAll(x => x.Name == player.Name);
            }

            var currentQueuePosition = Data.Queue.FindIndex(x => x.ToIrcNameFormat() == player.Name.ToIrcNameFormat());

            // We don't want to restore the position if the player currently is host, or is at the end of the queue.
            if (currentQueuePosition < 1 || currentQueuePosition == context.MultiplayerLobby.Players.Count - 1)
            {
                return;
            }
            
            Data.PreviousQueueRecords.Add(new HostQueueBehaviorData.PlayerPreviousQueueRecord()
            {
                Name = player.Name,
                Position = currentQueuePosition,
                Time = DateTime.UtcNow
            });
        }
        
        private void RestoreQueuePosition(MultiplayerPlayer player)
        {
            // Remove any records that are older than 2 minutes, since we don't want to restore them
            Data.PreviousQueueRecords.RemoveAll(x => DateTime.UtcNow > x.Time.AddMinutes(2));
            
            var previousRecord = Data.PreviousQueueRecords.FirstOrDefault(x => x.Name == player.Name);
            if (previousRecord == null || previousRecord.Position >= Data.Queue.Count)
            {
                return;
            }
            
            Data.Queue.RemoveAll(x => x == player.Name);
            Data.Queue.Insert(previousRecord.Position, player.Name);
            
            Data.PreviousQueueRecords.Remove(previousRecord);
        }
        
        /// <summary>
        /// Ensures the queue is valid by removing any players that are no longer in the lobby,
        /// or adding any players that are in the lobby but not in the queue.
        /// </summary>
        private async Task EnsureQueueValid()
        {
            // Remove any players from the queue that are no longer in the lobby
            foreach (var player in Data.Queue.ToList().Where(player => context.MultiplayerLobby.Players.All(x => x.Name.ToIrcNameFormat() != player.ToIrcNameFormat())))
            {
                Data.Queue.Remove(player);
            }

            // Add any players that are in the lobby but not in the queue
            foreach (var multiplayerPlayer in context.MultiplayerLobby.Players.Where(multiplayerPlayer => !Data.Queue.Any(x => x.ToIrcNameFormat() == multiplayerPlayer.Name.ToIrcNameFormat())))
            {
                var userRepo = new UserRepository();
                var user = await userRepo.FindOrCreateUser(multiplayerPlayer.Name);
                
                // Make sure we don't add any banned players to the queue
                if (user.Bans.Count != 0)
                {
                    continue;
                }
                
                Data.Queue.Add(multiplayerPlayer.Name);
            }
            
            // Remove any duplicates from the queue (shouldn't happen, but just in case)
            Data.Queue = Data.Queue.Distinct().ToList();

            ApplyRoomHost();
        }

        /// <summary>
        /// Will check for any host bans or auto-skip settings and return whether the player is a valid host candidate.
        /// </summary>
        private static async Task<bool> IsPlayerHostCandidate(string playerName)
        {
            using var userRepository = new UserRepository();
            var user = await userRepository.FindOrCreateUser(playerName);

            return user.Bans.Count == 0 && !user.AutoSkipEnabled;
        }
    }
}