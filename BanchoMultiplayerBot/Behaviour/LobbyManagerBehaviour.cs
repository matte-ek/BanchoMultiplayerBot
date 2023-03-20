using BanchoSharp;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// This behaviour will make sure the room name, size
/// password and game mode is set correctly.
/// </summary>
public class LobbyManagerBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    
    private DateTime _lastSettingsUpdateReceivedTime;

    // I really, really hate this but whatever.
    private int _mpSettingsAttempts = 0;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;

        _lobby.MultiplayerLobby.OnMatchStarted += OnMatchStarted;
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinishedOrAborted;
        _lobby.MultiplayerLobby.OnMatchAborted += OnMatchFinishedOrAborted;
        _lobby.MultiplayerLobby.OnSettingsUpdated += OnRoomSettingsUpdated;

        _lobby.OnAdminMessage += OnAdminMessage;

        _lobby.OnLobbyChannelJoined += RunSettingsCommand;
        
        // Quick temporary fix for an issue within BanchoSharp, that causes players with more than 16 characters to have duplicates.
        _lobby.MultiplayerLobby.OnPlayerDisconnected += player =>
        {
            var playerName = player.Player.Name;
            
            if (playerName.Length <= 16)
                return;
            
            var playerNameShorted = playerName[..16];
            var duplicatePlayer = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == playerNameShorted);

            if (duplicatePlayer is null)
                return;

            Log.Warning($"Duplicate player found {playerName} -> {duplicatePlayer.Name}, removing.");

            _lobby.MultiplayerLobby.Players.Remove(duplicatePlayer);
        };
    }

    private void OnAdminMessage(BanchoSharp.Interfaces.IPrivateIrcMessage message)
    {
    }

    private void OnMatchStarted()
    {
        // Automatically abort the match if it's started with 0 players, can happen if a player leaves between readying up
        // and the bot auto starting the match.
        if (_lobby.MultiplayerLobby.Players.Count == 0 && !_lobby.IsRecovering)
        {
            _lobby.SendMessage("!mp abort");
        }
    }

    private void OnMatchFinishedOrAborted()
    {
        // Run "!mp settings" to receive updated information from Bancho.
        RunSettingsCommand();
    }

    private void OnRoomSettingsUpdated()
    {
        // At this point we should have received updated information
        // from "!mp settings"
        _lastSettingsUpdateReceivedTime = DateTime.Now;
        _mpSettingsAttempts = 0;

        EnsureRoomName();
        EnsureRoomSize();
        EnsureRoomPassword();
        EnsureRoomMods();
    }

    private void EnsureRoomName()
    {
        if (_lobby.MultiplayerLobby.Name == _lobby.Configuration.Name)
            return;

        _lobby.SendMessage($"!mp name {_lobby.Configuration.Name}");
    }

    private void EnsureRoomSize()
    {
        if (_lobby.Configuration.Size == null)
            return;
        if (_lobby.IsRecovering)
            return;
            
        // We cannot verify anything here, so just update it all the time.
        
        _lobby.SendMessage($"!mp set {(int)(_lobby.Configuration.TeamMode ?? LobbyFormat.HeadToHead)} {(int)(_lobby.Configuration.ScoreMode ?? WinCondition.Score)} {_lobby.Configuration.Size}");
    }

    private void EnsureRoomPassword()
    {
        if (_lobby.Configuration.Password == null)
            return;
        if (_lobby.IsRecovering)
            return;

        // We cannot verify anything here either.

        _lobby.SendMessage($"!mp password {_lobby.Configuration.Password}");
    }

    private void EnsureRoomMods()
    {
        if (_lobby.Configuration.Mods == null)
            return;

        try
        {
            Mods desiredMods = 0;

            foreach (var modName in _lobby.Configuration.Mods)
            {
                desiredMods |= (Mods)Enum.Parse(typeof(Mods), modName);
            }

            if (_lobby.MultiplayerLobby.Mods == desiredMods)
            {
                return;
            }

            var modsCommandNonSpacing = desiredMods.ToAbbreviatedForm(false);

            if (modsCommandNonSpacing == "None")
            {
                if ((desiredMods & Mods.Freemod) != 0)
                {
                    _lobby.SendMessage($"!mp mods Freemod");
                }

                return;
            }

            // This has to be one of the stupidest things I've written in a while

            var modsCommand = "";
            bool newMod = false;

            foreach (var c in modsCommandNonSpacing)
            {
                modsCommand += c;

                if (newMod)
                {
                    modsCommand += ' ';
                    newMod = false;
                    continue;
                }

                newMod = true;
            }

            _lobby.SendMessage($"!mp mods {modsCommand}");
        }
        catch (Exception e)
        {
            Log.Error($"Error during EnsureRoomMods(): {e.Message}");
        }
    }

    private void RunSettingsCommand()
    {
        _lobby.SendMessage("!mp settings");

        Task.Run(EnsureSettingsSent);
    }

    // This is rather stupid but it'll ensure that the "!mp settings" command does in-fact get executed
    // which it for some reasons just don't want to sometimes.
    private async Task EnsureSettingsSent()
    {
        await Task.Delay(5000);

        if (DateTime.Now - _lastSettingsUpdateReceivedTime > TimeSpan.FromSeconds(15))
        {
            _lobby.SendMessage("!mp settings");

            // If we still have some attempts left, then check if it got ran successfully again
            // I hate the fact that I even have to do this but I have no clue as to why it doesn't get ran
            // in the first place. Seems to only be happening with `!mp settings` so I am assuming something Bancho related?
            if (_mpSettingsAttempts < 10)
            {
                _mpSettingsAttempts++;

                _ = Task.Run(EnsureSettingsSent);
            }
        }
    }
}