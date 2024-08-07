using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Events;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class HostQueueBehavior(BotEventContext context) : IBehavior
    {
        private readonly BehaviorDataProvider<HostQueueBehaviorData> _dataProvider = new(context.Lobby);
        private HostQueueBehaviorData Data => _dataProvider.Data;

        [BotEvent(BotEventType.BotStarted)]
        public async Task Setup()
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
                Log.Warning("HostQueueBehavior: Player {PlayerName} is already in the queue during join event", player.Name);
                return;
            }
            
            Data.Queue.Add(player.Name);
            
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
            
            Data.Queue.Remove(player.Name);
            
            ApplyRoomHost();
        }

        [BotEvent(BotEventType.BehaviourEvent, "LobbyManagerMatchFinished")]
        public async Task OnManagerMatchFinished()
        {
            await SkipHost();
        }
        
        [BotEvent(BotEventType.CommandExecuted, "Queue")]
        public void OnQueueCommandExecuted()
        {
        }
        
        [BotEvent(BotEventType.CommandExecuted, "QueuePosition")]
        public void OnQueuePositionCommandExecuted()
        {
        }
        
        [BotEvent(BotEventType.CommandExecuted, "Skip")]
        public async Task OnSkipCommandExecuted()
        {
        }
        
        [BotEvent(BotEventType.CommandExecuted, "ForceSkip")]
        public async Task OnForceSkipCommandExecuted()
        {
        }
        
        [BotEvent(BotEventType.CommandExecuted, "SetHost")]
        public async Task OnSetHostCommandExecuted()
        {
        }
        
        [BotEvent(BotEventType.CommandExecuted, "SetQueuePosition")]
        public async Task OnSetQueuePositionCommandExecuted()
        {
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
            foreach (var multiplayerPlayer in context.MultiplayerLobby.Players.Where(multiplayerPlayer => !Data.Queue.Contains(multiplayerPlayer.Name.ToIrcNameFormat())))
            {
                var userRepo = new UserRepository();
                var user = await userRepo.FindOrCreateUser(multiplayerPlayer.Name);
                
                // Make sure we don't add any banned players to the queue
                if (user.Bans.Count == 0)
                {
                    continue;
                }
                
                Data.Queue.Add(multiplayerPlayer.Name);
            }

            ApplyRoomHost();
        }

        /// <summary>
        /// Will check for any host bans or auto-skip settings and return whether the player is a valid host candidate.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private static async Task<bool> IsPlayerHostCandidate(string playerName)
        {
            using var userRepository = new UserRepository();
            var user = await userRepository.FindOrCreateUser(playerName);

            return user.Bans.Count == 0 && !user.AutoSkipEnabled;
        }
    }
}