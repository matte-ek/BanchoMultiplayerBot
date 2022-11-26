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

    private bool _afkTimerActive;
    private Task? _afkTimerTask;
    private CancellationTokenSource? _afkTimerCancellationToken;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.MultiplayerLobby.OnHostChanged += OnHostChanged;
        _lobby.MultiplayerLobby.OnHostChangingMap += OnHostChangingMap;
        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        _lobby.Bot.Client.OnPrivateMessageReceived += OnPrivateMessageReceived;
    }

    private void OnMatchStarted()
    {
        AbortTimer();
    }

    private void OnPrivateMessageReceived(IPrivateIrcMessage msg)
    {
        if (!(msg.IsDirect && msg.IsBanchoBotMessage))
            return;
        if (!msg.Content.StartsWith("Stats for ("))
            return;

        try
        {
            var playerNameBegin = msg.Content.IndexOf('(') + 1;
            var playerNameEnd = msg.Content.IndexOf(')');
            var playerName = msg.Content[playerNameBegin..playerNameEnd];

            var status = "Unknown";

            if (msg.Content.Contains("is Multiplayer") || msg.Content.Contains("is Multiplayer"))
                status = "Multiplayer";
            if (msg.Content.Contains("is Idle"))
                status = "Idle";
            if (msg.Content.Contains("is Afk"))
                status = "Afk";

            Console.WriteLine($"Parsed status {status} for {playerName}");

            if (playerName != _lobby.MultiplayerLobby.Host?.Name)
            {
                return;
            }

            if (status == "Afk")
            {
                Log.Information("Kicking host due to AFK.");
                _lobby.SendMessage($"!mp kick {_lobby.GetPlayerIdentifier(playerName)}");
                return;
            }
            
            StartTimer(120);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void OnHostChangingMap()
    {
        AbortTimer();
    }

    private void OnHostChanged(MultiplayerPlayer player)
    {
        StartTimer();
    }

    private void StartTimer(int timeoutTime = 30)
    {
        if (_afkTimerActive)
        {
            _afkTimerActive = false;
            
            AbortTimer();
        }

        _afkTimerTask?.Wait(100);
        
        _afkTimerCancellationToken?.Dispose();
        _afkTimerCancellationToken = new CancellationTokenSource();

        _afkTimerTask = Task.Delay(1000 * timeoutTime, _afkTimerCancellationToken.Token).ContinueWith(x =>
        {
            try
            {
                if (_afkTimerCancellationToken.IsCancellationRequested || !_afkTimerActive)
                {
                    return;
                }

                var name = _lobby.MultiplayerLobby.Host?.Name;

                if (name == null)
                {
                    return;
                }

                _lobby.Bot.Client.SendPrivateMessageAsync("BanchoBot", $"!stat {name.Replace(' ', '_')}");
            }
            catch (Exception)
            {
                // ignored
            }
        });
        
        _afkTimerActive = true;
    }
    
    private void AbortTimer()
    {
        _afkTimerActive = false;
        
        try
        { 
            _afkTimerCancellationToken?.Cancel(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}