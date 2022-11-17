using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Behaviour;

public class AutoStartBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerStartVote = null!;

    private bool _startTimerActive = false;
    private Task? _startTimerTask;
    private Task? _startTimerWarningTask;
    private CancellationTokenSource? _startTimerCancellationToken;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerStartVote = new PlayerVote(_lobby, "Vote to start");

        _lobby.MultiplayerLobby.OnAllPlayersReady += () =>
        {
            if (_lobby.Bot.Configuration.AutoStartAllPlayersReady == null || !_lobby.Bot.Configuration.AutoStartAllPlayersReady.Value)
            {
                return;
            }
            
            _lobby.SendMessage("!mp start");
        };

        _lobby.MultiplayerLobby.OnMatchStarted += () =>
        {
            AbortTimer();

            _playerStartVote.Reset();
        };
            
        _lobby.OnUserMessage += OnUserMessage;

        var mapManagerBehaviour = _lobby.Behaviours.Find(x => x.GetType() == typeof(MapManagerBehaviour));
        if (mapManagerBehaviour != null)
        {
            ((MapManagerBehaviour)mapManagerBehaviour).OnNewAllowedMap += OnNewAllowedMap;
        }
    }

    private void OnNewAllowedMap()
    {
        if (_lobby.Bot.Configuration.EnableAutoStartTimer == null ||
            !_lobby.Bot.Configuration.EnableAutoStartTimer.Value ||
            _lobby.Bot.Configuration.AutoStartTimerTime == null) 
            return;

        StartTimer(_lobby.Bot.Configuration.AutoStartTimerTime.Value);
    }

    private void OnUserMessage(IPrivateIrcMessage message)
    {
        // Kind of a stupid fix to prevent loop backs
        bool isPlayer = message.Sender != _lobby.Bot.Configuration.Username;

        if (message.Content.StartsWith("!start") || (message.Content.StartsWith("!mp start") && isPlayer))
        {
            try
            {
                int requestedTime;

                if (message.Content.StartsWith("!start"))
                    requestedTime = int.Parse(message.Content.Split("!start ")[1]);
                else
                    requestedTime = int.Parse(message.Content.Split("!mp start ")[1]);

                if (_lobby.MultiplayerLobby.Host is not null)
                {
                    if (message.Sender == _lobby.MultiplayerLobby.Host.Name)
                    {
                        StartTimer(requestedTime);

                        return;
                    }
                }

                var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == message.Sender);
                if (player is not null)
                {
                    if (_playerStartVote.Vote(player))
                    {
                        _lobby.SendMessage("!mp start");

                        return;
                    }
                }
            }
            catch (Exception)
            {
                _lobby.SendMessage("Usage: !start <seconds>");
            }
        }

        if (message.Content.StartsWith("!stop"))
        {
            if (_lobby.MultiplayerLobby.Host is not null)
            {
                if (message.Sender == _lobby.MultiplayerLobby.Host.Name)
                {
                    AbortTimer();
                }
            }
        }
    }

    private void StartTimer(int length)
    {
        if (_startTimerActive)
        {
            _startTimerActive = false;
            
            AbortTimer();
        }

        if (length <= 1 || length >= 500)
            return;

        _startTimerTask?.Wait();
        _startTimerWarningTask?.Wait();
        
        Console.WriteLine("(Re)starting timer.");
        
        _startTimerCancellationToken?.Dispose();
        _startTimerCancellationToken = new CancellationTokenSource();

        _startTimerTask = Task.Delay(1000 * length, _startTimerCancellationToken.Token).ContinueWith(x => 
        {
            if (_startTimerCancellationToken.IsCancellationRequested || !_startTimerActive)
            {
                return;
            }
            
            Console.WriteLine("Timer ended! Starting game...");
            
           // _lobby.SendMessage("!mp start");
        });

        if (length > 10)
        {
            _startTimerWarningTask = Task.Delay(1000 * (length - 10), _startTimerCancellationToken.Token).ContinueWith(x => 
            {
                if (_startTimerCancellationToken.IsCancellationRequested || !_startTimerActive)
                {
                    return;
                }
                
                _lobby.SendMessage("Starting match in 10 seconds, use !stop to abort!");
            });
        }

        _lobby.SendMessage($"Queued to start match in {length} seconds! Use !stop to abort");

        _startTimerActive = true;
    }

    private void AbortTimer()
    {
        _startTimerActive = false;
        
        try
        { 
            _startTimerCancellationToken?.Cancel(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}