using BanchoSharp;

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
        // TODO: Allow mods through configuration

        // Currently, make sure only the freemod bit is set.
        if (_lobby.MultiplayerLobby.Mods == BanchoSharp.Multiplayer.Mods.Freemod)
        {
            return;
        }

        _lobby.SendMessage($"!mp mods Freemod");
    }
}