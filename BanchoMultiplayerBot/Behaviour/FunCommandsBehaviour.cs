using BanchoMultiplayerBot.Database.Bot.Repositories;
using BanchoMultiplayerBot.Extensions;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class FunCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private bool _flushedPlaytime = false;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        _lobby.OnUserMessage += OnUserMessage;
    }

    public async Task FlushPlaytime()
    {
        using var userRepository = new UserRepository();

        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

            user.Playtime += (int)(DateTime.Now - player.JoinTime).TotalSeconds;
        }

        await userRepository.Save();

        _flushedPlaytime = true;
    }

    private async void OnUserMessage(IPrivateIrcMessage msg)
    {
        try
        {
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == msg.Sender);

            if (player is null)
            {
                return;
            }

            if (msg.Content.ToLower().Equals("!playtime"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                var currentPlaytime = DateTime.Now - player.JoinTime;
                var totalPlaytime = TimeSpan.FromSeconds(user.Playtime) + currentPlaytime; // We add current play-time since it's only appended after the player disconnects.

                _lobby.SendMessage(
                    _lobby.Bot.StartTime.AddMinutes(2) >= player.JoinTime
                        ? $"{msg.Sender} has been here since last bot restart, {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} in total)"
                        : $"{msg.Sender} has been here for {currentPlaytime:h' hours 'm' minutes 's' seconds'} ({totalPlaytime:d' days 'h' hours 'm' minutes 's' seconds'} in total)");
            }

            if (msg.Content.ToLower().Equals("!playstats"))
            {
                using var userRepository = new UserRepository();
                var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

                _lobby.SendMessage($"{msg.Sender} has played {user.MatchesPlayed} matches with a total of {user.NumberOneResults} #1's");
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async void OnMatchFinished()
    {
        using var userRepository = new UserRepository();

        var highestScorePlayer = _lobby.MultiplayerLobby.Players.Where(x => x.Passed == true).MaxBy(x => x.Score);

        if (_lobby.MultiplayerLobby.Players.Count >= 3 && highestScorePlayer is not null)
        {
            var user = await userRepository.FindUser(highestScorePlayer.Name) ?? await userRepository.CreateUser(highestScorePlayer.Name);

            user.NumberOneResults++;
        }

        foreach (var player in _lobby.MultiplayerLobby.Players)
        {
            var user = await userRepository.FindUser(player.Name) ?? await userRepository.CreateUser(player.Name);

            if (player.Score > 0)
                user.MatchesPlayed++;
        }

        await userRepository.Save();
    }

    private async void OnPlayerDisconnected(PlayerDisconnectedEventArgs args)
    {
        if (_flushedPlaytime)
            return;

        using var userRepository = new UserRepository();
        var user = await userRepository.FindUser(args.Player.Name) ?? await userRepository.CreateUser(args.Player.Name);

        user.Playtime += (int)(DateTime.Now - args.Player.JoinTime).TotalSeconds;

        await userRepository.Save();
    }
}