using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// Attempts to kick AFK players by detecting their status via the '!stat' command.
/// </summary>
public class AntiAfkBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private bool _afkTimerTaskActive = true;

    private bool _afkTimerActive;
    private DateTime _afkTimerTime;

    ~AntiAfkBehaviour()
    {
        _afkTimerTaskActive = false;
    }

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

         Task.Run(AfkTimerTask);

        _lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        _lobby.MultiplayerLobby.OnHostChangingMap += OnHostChangingMap;
        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        _lobby.OnBanchoMessage += OnBanchoMessage;
    }

    private void OnBanchoMessage(IPrivateIrcMessage msg)
    {
        if (!msg.IsDirect)
            return;
        if (!msg.Content.StartsWith("Stats for ("))
            return;

        try
        {
            var playerNameBegin = msg.Content.IndexOf('(') + 1;
            var playerNameEnd = msg.Content.IndexOf(')');
            var playerName = msg.Content[playerNameBegin..playerNameEnd];

            if (playerName != _lobby.MultiplayerLobby.Host?.Name)
            {
                return;
            }

            if (msg.Content.Contains("is Afk"))
            {
                Log.Information("Kicking host due to AFK.");
                
                _lobby.SendMessage($"!mp kick {_lobby.GetPlayerIdentifier(playerName)}");

                return;
            }
            
            StartTimer(60);
        }
        catch (Exception e)
        {
            Log.Error($"Failure in parsing BanchoBot stats response, {e}");
        }    
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        StartTimer();
    }

    private void OnMatchStarted()
    {
        _afkTimerActive = false;
    }

    private void OnHostChangingMap()
    {
        _afkTimerActive = false;
    }

    private void StartTimer(int timeoutTime = 30)
    {
        if (_afkTimerActive)
        {
            _afkTimerActive = false;
        }

        _afkTimerTime = DateTime.Now.AddSeconds(timeoutTime);
        _afkTimerActive = true;
    }
    
    private async Task AfkTimerTask()
    {
        while (_afkTimerTaskActive)
        {
            await Task.Delay(100);

            if (_afkTimerActive && DateTime.Now >= _afkTimerTime)
            {
                _afkTimerActive = false;

                if (_lobby.MultiplayerLobby.Players.Count == 0)
                {
                    continue;
                }

                var name = _lobby.MultiplayerLobby.Host?.Name;

                if (name == null)
                {
                    continue;
                }

                _lobby.Bot.SendMessage("BanchoBot", $"!stat {name.Replace(' ', '_')}");
            }
        }
    }
}