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
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinishedOrAborted;
        _lobby.MultiplayerLobby.OnMatchAborted += OnMatchFinishedOrAborted;
        _lobby.MultiplayerLobby.OnSettingsUpdated += OnRoomSettingsUpdated;

        _lobby.OnAdminMessage += OnAdminMessage;

        _lobby.OnLobbyChannelJoined += () =>
        {
            _lobby.SendMessage("!mp settings");
        };
        
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

    private void OnMatchFinishedOrAborted()
    {
        // Run "!mp settings" to receive updated information from Bancho.
        _lobby.SendMessage("!mp settings");
    }

    private void OnRoomSettingsUpdated()
    {
        // At this point we should have received updated information
        // from "!mp settings"

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
        
        _lobby.SendMessage($"!mp set 0 0 {_lobby.Configuration.Size}");
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
        if (_lobby.Configuration.SelectedMods == null)
            return;

        try
        {
            Mods desiredMods = 0;

            foreach (var modName in _lobby.Configuration.SelectedMods)
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
}