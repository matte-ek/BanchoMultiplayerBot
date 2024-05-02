using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Database.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class BanBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private Dictionary<string, PlayerJoinBan> _playerJoinRecords = new();
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnAdminMessage += OnAdminMessage;
        _lobby.MultiplayerLobby.OnPlayerJoined += OnPlayerJoined;
    }

    public static async Task<IEnumerable<PlayerBan>> GetActivePlayerBans(string playerName)
    {
        try
        {
            using var userRepository = new UserRepository();
            
            var user = await userRepository.FindUser(playerName);

            return user?.Bans.Where(x => x.Active && (x.Expire == null || x.Expire > DateTime.Now)).ToList() ?? Enumerable.Empty<PlayerBan>();
        }
        catch (Exception e)
        {
            Log.Error($"Expcetion while querying player bans: {e}");

            return Enumerable.Empty<PlayerBan>();
        }
    }

    private async void OnAdminMessage(IPrivateIrcMessage e)
    {
        try
        {
            using var userRepository = new UserRepository();
            using var banRepository = new PlayerBanRepository();

            if (e.Content.StartsWith("!ban "))
            {
                var args = e.Content.Split(' ');

                var playerName = args[1];
                var hostBanOnly = args[2];
                var expireTime = args.Length >= 4 ? args[3] : null;
                var reason = args.Length >= 5 ? e.Content[string.Join(' ', args, 0, 4).Length..].TrimStart() : null;

                if (!playerName.Any())
                {
                    return;
                }
            
                var user = await userRepository.FindUser(playerName);

                if (user == null)
                {
                    _lobby.SendMessage("Cannot find user.");
                    return;
                }

                var hostBan = hostBanOnly.ToLower() == "yes" || hostBanOnly.ToLower() == "true";
                
                await banRepository.CreateBan(
                    user,
                    hostBan,
                    reason,
                    expireTime != null ? DateTime.Now.AddDays(int.Parse(expireTime)) : null);
                
                // If the user is banned from the lobby, we might as well kick the user immediately
                if (!hostBan)
                {
                    _lobby.SendMessage($"!mp kick {playerName.ToIrcNameFormat()}");
                }
                else
                {
                    // Or we'll have to make sure the AHR system gets notified of the news.
                    if (_lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour)) is 
                        AutoHostRotateBehaviour autoHostRotateBehaviour)
                    {
                        await autoHostRotateBehaviour.RefreshPlayerBanStates();
                    }
                }
                
                _lobby.SendMessage("Player was successfully put on ban list.");
            }

            if (e.Content.StartsWith("!removeban "))
            {
                var args = e.Content.Split(' ');
                var playerName = args[1];
                
                var user = await userRepository.FindUser(playerName);
                var bans = user?.Bans.Where(x => x.Active && (x.Expire == null || x.Expire > DateTime.Now)).ToList() ?? Enumerable.Empty<PlayerBan>();

                foreach (var ban in bans)
                {
                    ban.Active = false;
                }

                await userRepository.Save();
            }
        }
        catch (Exception exception)
        {
            _lobby.SendMessage("Usage: !ban <player> <host-ban-only> <expire-time> <reason>");
            Log.Error($"BanBehaviour::OnAdminMessage(): {exception}");
        }

        try
        {
            using var mapBanRepository = new MapBanRepository();
            
            if (e.Content.StartsWith("!banmapset "))
            {
                var args = e.Content.Split(' ');

                await mapBanRepository.AddMapBan(int.Parse(args[1]), null);
                await mapBanRepository.Save();
            }
            
            if (e.Content.StartsWith("!banmap "))
            {
                var args = e.Content.Split(' ');

                await mapBanRepository.AddMapBan(null, int.Parse(args[1]));
                await mapBanRepository.Save();
            }
        }
        catch (Exception exception)
        {
            _lobby.SendMessage("Usage: !banmap[set] <id>");
            Log.Error($"BanBehaviour::OnAdminMessage(): {exception}");
        }
    }

    private async void OnPlayerJoined(MultiplayerPlayer player)
    {
        var bans = await GetActivePlayerBans(player.Name);

        if (!bans.Any(x => !x.HostBan))
        {
            return;
        }

        // We do this player join record crap since we do not ever really want to use the `!mp ban` command
        // unless we absolutely have to, since there is no "!mp unban" command, so the player will remain banned
        // until the lobby gets recreated. This prevents us from controlling whenever a user should be unbanned or
        // even worse, if a user was wrongfully banned it may not be possible to unban the player. We do still
        // keep a "fail safe" to `!mp ban` if the player keeps rejoining quickly in a small timespan since it
        // may otherwise be abused to spam the chat with "!mp kick" messages.

        if (!_playerJoinRecords.ContainsKey(player.Name))
        {
            _playerJoinRecords.Add(player.Name, new PlayerJoinBan
            {
                Name = player.Name,
                Frequency = 0,
                LastJoinTime = DateTime.Now,
            });
        }

        var record = _playerJoinRecords[player.Name];

        if ((DateTime.Now - record.LastJoinTime).TotalMinutes > 15)
        {
            record.Frequency = 0;
        }

        record.Frequency++;

        var method = record.Frequency > 5 ? "ban" : "kick";

        _lobby.SendMessage($"!mp {method} {player.Name.ToIrcNameFormat()}");
    }
}