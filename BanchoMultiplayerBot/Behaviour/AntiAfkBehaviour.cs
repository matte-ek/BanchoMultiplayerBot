using BanchoMultiplayerBot.Extensions;
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

    private Task _afkTimerTask = null!;
    private bool _afkTimerTaskActive = true;
    
    private bool _afkTimerActive;
    private DateTime _afkTimerTime;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _afkTimerTask = Task.Run(AfkTimerTask);

        _lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        _lobby.MultiplayerLobby.OnHostChangingMap += OnHostChangingMap;
        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        _lobby.OnBanchoMessage += OnBanchoMessage;
    }

    public void Shutdown()
    {
        _lobby.MultiplayerLobby.OnHostChanged -= OnHostChanged;
        _lobby.MultiplayerLobby.OnHostChangingMap -= OnHostChangingMap;
        _lobby.MultiplayerLobby.OnMatchStarted -= OnMatchStarted;
        _lobby.OnBanchoMessage -= OnBanchoMessage;
        
        _afkTimerTaskActive = false;
        _afkTimerTask.Wait();
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        StartTimer();
    }

    private void OnMatchStarted()
    {
        StopTimer();
    }

    private void OnHostChangingMap()
    {
        StopTimer();
    }
    
    private void OnBanchoMessage(IPrivateIrcMessage msg)
    {
        try
        {
            if (!msg.IsDirect)
                return;
            if (!msg.Content.StartsWith("Stats for ("))
                return;
            
            var playerNameBegin = msg.Content.IndexOf('(') + 1;
            var playerNameEnd = msg.Content.IndexOf(')');
            var playerName = msg.Content[playerNameBegin..playerNameEnd];

            if (playerName != _lobby.MultiplayerLobby.Host?.Name)
                return;

            if (msg.Content.Contains("is Afk"))
            {
                Log.Information("Kicking host due to AFK.");
                
                _lobby.SendMessage($"!mp kick {_lobby.GetPlayerIdentifier(playerName)}");

                return;
            }
            
            // Player is not reported as AFK yet, check again after 60 seconds
            StartTimer(60);
        }
        catch (Exception e)
        {
            Log.Error($"Failure in parsing BanchoBot stats response, {e}");
        }    
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

    private void StopTimer()
    {
        _afkTimerActive = false;
    }
    
    private async Task AfkTimerTask()
    {
        while (_afkTimerTaskActive)
        {
            await Task.Delay(100);

            if (!_afkTimerActive || DateTime.Now < _afkTimerTime)
            {
                continue;
            }
            
            _afkTimerActive = false;

            var name = _lobby.MultiplayerLobby.Host?.Name;
            
            if (_lobby.MultiplayerLobby.Players.Count == 0 ||
                name == null)
            {
                continue;
            }

            _lobby.Bot.SendMessage("BanchoBot", $"!stat {name.ToIrcNameFormat()}");
        }
    }
}