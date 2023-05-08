using BanchoMultiplayerBot.Extensions;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class AutoStartBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerStartVote = null!;

    private bool _startTimerTaskActive = true;

    private bool _startTimerActive;
    private DateTime _startTimer;

    private bool _shouldSendWarningMessage;
    private bool _sentWarningMessage;

    ~AutoStartBehaviour()
    {
        _startTimerTaskActive = false;
    }

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerStartVote = new PlayerVote(_lobby, "Vote to start");

        Task.Run(StartTimerTask); 

        _lobby.MultiplayerLobby.OnAllPlayersReady += () =>
        {
            if (_lobby.Bot.Configuration.AutoStartAllPlayersReady == null || !_lobby.Bot.Configuration.AutoStartAllPlayersReady.Value)
            {
                return;
            }
            
            _lobby.SendMessage("!mp start");
            
            AbortTimer();
        };

        _lobby.MultiplayerLobby.OnMatchStarted += () =>
        {
            AbortTimer();

            _playerStartVote.Reset();
        };

        _lobby.MultiplayerLobby.OnHostChanged += player =>
        {
            AbortTimer();
            
            _playerStartVote.Reset();
        };

        _lobby.MultiplayerLobby.OnHostChangingMap += AbortTimer;
        _lobby.OnUserMessage += OnUserMessage;
        _lobby.OnAdminMessage += OnAdminMessage;

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

    private void OnAdminMessage(IPrivateIrcMessage message)
    {
        if (message.Content.ToLower().StartsWith("!stop"))
        {
            AbortTimer();
        }
    }

    private void OnUserMessage(IPrivateIrcMessage message)
    {
        // Kind of a stupid fix to prevent loop backs
        bool isPlayer = message.Sender != _lobby.Bot.Configuration.Username;

        if (message.Content.ToLower().StartsWith("!start") || (message.Content.ToLower().StartsWith("!mp start") && isPlayer))
        {
            try
            {
                if (_lobby.MultiplayerLobby.Host is not null)
                {
                    if (message.Sender == _lobby.MultiplayerLobby.Host.Name.ToIrcNameFormat())
                    {
                        // If the user ran '!start' without any arguments,
                        // start the match immediately.
                        if (message.Content.ToLower().Equals("!start") || message.Content.ToLower().Equals("!mp start"))
                        {
                            _lobby.SendMessage("!mp start");
                            return;
                        }
                        
                        int requestedTime;

                        if (message.Content.ToLower().StartsWith("!start"))
                            requestedTime = int.Parse(message.Content.ToLower().Split("!start ")[1]);
                        else
                            requestedTime = int.Parse(message.Content.ToLower().Split("!mp start ")[1]);

                        StartTimer(requestedTime);

                        return;
                    }
                }

                var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name.ToIrcNameFormat() == message.Sender);
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
                _lobby.SendMessage("Usage: !start [<seconds>]");
            }
        }

        if (message.Content.ToLower().StartsWith("!stop"))
        {
            if (_lobby.MultiplayerLobby.Host is not null)
            {
                if (message.Sender == _lobby.MultiplayerLobby.Host.Name.ToIrcNameFormat())
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
            AbortTimer();
        }

        if (length <= 1 || length >= 500)
            return;
             
        // This was previously implemented with Task.Delay, and a cancellation token to cancel the delay
        // task if we had to abort the timer. This however for some reason didn't exactly work all the time,
        // not sure why yet. As a result, this has been changed to a always on task instead.

        _startTimer = DateTime.Now.AddSeconds(length);
        _shouldSendWarningMessage = length > 10;
        _sentWarningMessage = false;
        _startTimerActive = true;

        _lobby.SendMessage($"Queued to start match in {length} seconds, use !stop to abort");
    }

    private void AbortTimer()
    {
        _startTimerActive = false;
        _shouldSendWarningMessage = false;
        _sentWarningMessage = false;
    }

    private async Task StartTimerTask()
    {
        while (_startTimerTaskActive)
        {
            await Task.Delay(100);

            if (!_startTimerActive)
            {
                continue;
            }

            if (_shouldSendWarningMessage && !_sentWarningMessage && (DateTime.Now.AddSeconds(10)) >= _startTimer)
            {
                _sentWarningMessage = true;

                _lobby.SendMessage("Starting match in 10 seconds, use !stop to abort");

                continue;
            }

            if (DateTime.Now < _startTimer)
            {
                continue;
            }

            if (_lobby.MultiplayerLobby.Players.Count == 0)
            {
                _startTimerActive = false;
                continue;
            }

            _startTimerActive = false;

            _lobby.SendMessage("!mp start");
        }
    }
}