using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Providers;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviors
{
    public class BanBehavior(BotEventContext context) : IBehavior
    {
        private readonly BehaviorDataProvider<BanBehaviorData> _dataProvider = new(context.Lobby);
        private BanBehaviorData Data => _dataProvider.Data;

        [BanchoEvent(BanchoEventType.OnPlayerJoined)]
        public async Task OnPlayerJoined(MultiplayerPlayer player)
        {
            var bans = await GetActivePlayerBans(player.Name);

            if (bans.All(x => x.HostBan))
            {
                return;
            }

            // We do this player join record crap since we do not ever really want to use the `!mp ban` command
            // unless we absolutely have to, since there is no "!mp unban" command, so the player will remain banned
            // until the lobby gets recreated. This prevents us from controlling whenever a user should be unbanned or
            // even worse, if a user was wrongfully banned it may not be possible to unban the player. We do still
            // keep a "fail-safe" to `!mp ban` if the player keeps rejoining quickly in a small timespan since it
            // may otherwise be abused to spam the chat with "!mp kick" messages.

            if (!Data.JoinedRecords.TryGetValue(player.Name, out BanBehaviorData.PlayerJoinedRecord? record))
            {
                record = new BanBehaviorData.PlayerJoinedRecord
                {
                    Name = player.Name,
                    Frequency = 0,
                    LastJoinTime = DateTime.UtcNow,
                };

                Data.JoinedRecords.Add(player.Name, record);
            }

            // If it has been more than 15 minutes since the last join, reset the frequency
            // we only care about short-term spam crap
            if ((DateTime.UtcNow - record.LastJoinTime).TotalMinutes > 15)
            {
                Log.Information("BanBehavior: Resetting join frequency for user '{User}'", player.Name);
                record.Frequency = 0;
            }

            record.Frequency++;
            record.LastJoinTime = DateTime.UtcNow;

            Log.Information("BanBehavior: Banned user '{User}' joined the room, current join frequency {Frequency}", player.Name, record.Frequency);

            var method = record.Frequency > 5 ? "ban" : "kick";

            context.SendMessage($"!mp {method} {player.Name.ToIrcNameFormat()}");
        }

        private static async Task<IEnumerable<PlayerBan>> GetActivePlayerBans(string playerName)
        {
            using var userRepository = new UserRepository();

            var user = await userRepository.FindUser(playerName);

            return user?.Bans.Where(x => x.Active && (x.Expire == null || x.Expire > DateTime.Now)).ToList() ?? Enumerable.Empty<PlayerBan>();
        }
    }
}
