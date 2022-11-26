using BanchoSharp;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class AutoStartBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private PlayerVote _playerStartVote = null!;

    private bool _startTimerTaskActive = true;
    private Task? _startTimerTask;

    private bool _startTimerActive = false;
    private DateTime _startTimer;

    private bool _shouldSendWarningMessage = false;
    private bool _sentWarningMessage = false;

    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _playerStartVote = new PlayerVote(_lobby, "Vote to start");

        _startTimerTask = Task.Run(StartTimerTask); 

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
                if (_lobby.MultiplayerLobby.Host is not null)
                {
                    if (message.Sender == _lobby.MultiplayerLobby.Host.Name)
                    {
                        // If the user ran '!start' without any arguments,
                        // start the match immediately.
                        if (message.Content.Equals("!start") || message.Content.Equals("!mp start"))
                        {
                            _lobby.SendMessage("!mp start");
                            return;
                        }
                        
                        int requestedTime;

                        if (message.Content.StartsWith("!start"))
                            requestedTime = int.Parse(message.Content.Split("!start ")[1]);
                        else
                            requestedTime = int.Parse(message.Content.Split("!mp start ")[1]);

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
            AbortTimer();
        }

        if (length <= 1 || length >= 500)
            return;
        
        Log.Information("(Re)starting timer.");
     
        // This was previously implemented with Task.Delay, and a cancellation token to cancel the delay
        // task if we had to abort the timer. This however for some reason didn't exactly work all the time,
        // not sure why yet. As a result, this has been changed to a always on task instead.

        _startTimer = DateTime.Now.AddSeconds(length);
        _shouldSendWarningMessage = length > 10;
        _sentWarningMessage = false;
        _startTimerActive = true;

        _lobby.SendMessage($"Queued to start match in {length} seconds! Use !stop to abort");
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

            if (_startTimerActive)
            {
                if (_shouldSendWarningMessage && !_sentWarningMessage && (DateTime.Now.AddSeconds(10)) >= _startTimer)
                {
                    _sentWarningMessage = true;

                    _lobby.SendMessage("Starting match in 10 seconds, use !stop to abort!");

                    continue;
                }

                if (DateTime.Now < _startTimer)
                {
                    continue;
                }

                _startTimerActive = false;

                _lobby.SendMessage("!mp start");
            }
        }
    }
}