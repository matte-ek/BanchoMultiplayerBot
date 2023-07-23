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

                await banRepository.CreateBan(
                    user,
                    hostBanOnly.ToLower() == "yes" || hostBanOnly.ToLower() == "true",
                    reason,
                    expireTime != null ? DateTime.Now.AddDays(int.Parse(expireTime)) : null);
                
                _lobby.SendMessage("Player was succesfully put on ban list.");
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

        if (bans.Any(x => !x.HostBan))
        {
            _lobby.SendMessage($"!mp ban {player.Name.ToIrcNameFormat()}");
        }
    }
}