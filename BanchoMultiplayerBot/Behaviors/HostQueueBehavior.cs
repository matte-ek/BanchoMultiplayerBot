using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Events;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Utilities;

namespace BanchoMultiplayerBot.Behaviors
{
    public class HostQueueBehavior(BotEventContext context) : IBehavior
    {
        private readonly BehaviorDataProvider<HostQueueBehaviorData> _dataProvider = new(context.Lobby);
        private HostQueueBehaviorData Data => _dataProvider.Data;

        [BotEvent(BotEventType.BotStarted)]
        public void Setup()
        {
            // Since we might be picking up where we left off, we need to ensure the queue is valid
            // since some players might have left while the bot was offline
            EnsureQueueValid();
        }
        
        [BanchoEvent(BanchoEventType.MatchStarted)]
        public void OnMatchStarted()
        {
            context.SendMessage("OnMatchStarted");
        }

        [BanchoEvent(BanchoEventType.MatchFinished)]
        public void OnMatchFinished()
        {
            context.SendMessage("OnMatchFinished");
        }

        [CommandEvent("skip")]
        public async Task OnSkipCommandExecuted()
        {
        }
        
        /// <summary>
        /// Will make sure the host is set to the first player in the queue, if it isn't already.
        /// Call this method whenever the queue is modified.
        /// </summary>
        private void ApplyRoomHost()
        {
            if (context.MultiplayerLobby.Host != null && 
                Data.Queue.FirstOrDefault()?.ToIrcNameFormat() == context.MultiplayerLobby.Host.Name.ToIrcNameFormat())
            {
                // Correct host is already set, no need to do anything
                return;
            }
            
            // Set the host to the first player in the queue
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
        private void EnsureQueueValid()
        {
            // Remove any players from the queue that are no longer in the lobby
            foreach (var player in Data.Queue.ToList().Where(player => context.MultiplayerLobby.Players.All(x => x.Name.ToIrcNameFormat() != player.ToIrcNameFormat())))
            {
                Data.Queue.Remove(player);
            }

            // Add any players that are in the lobby but not in the queue
            foreach (var multiplayerPlayer in context.MultiplayerLobby.Players.Where(multiplayerPlayer => !Data.Queue.Contains(multiplayerPlayer.Name.ToIrcNameFormat())))
            {
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
            
            if (user.Bans.Count != 0)
            {
                return false;
            }

            if (user.AutoSkipEnabled)
            {
                return false;
            }

            return true;
        }
    }
}