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
        
        _lobby.MultiplayerLobby.OnMatchFinished += OnMatchFinished;
        _lobby.MultiplayerLobby.OnSettingsUpdated += OnRoomSettingsUpdated;

        _lobby.OnLobbyChannelJoined += async () =>
        {
            await _lobby.SendMessageAsync("!mp settings");
        };
    }

    private async void OnMatchFinished()
    {
        // Run "!mp settings" to receive updated information from Bancho.
        await _lobby.SendMessageAsync("!mp settings");
    }

    private async void OnRoomSettingsUpdated()
    {   
        // At this point we should have received updated information
        // from "!mp settings"

        await EnsureRoomName();
        await EnsureRoomSize();
        await EnsureRoomPassword();
    }

    private async Task EnsureRoomName()
    {
        if (_lobby.MultiplayerLobby.Name == _lobby.Configuration.Name)
            return;

        await _lobby.SendMessageAsync($"!mp name {_lobby.Configuration.Name}");
    }

    private async Task EnsureRoomSize()
    {
        if (_lobby.Configuration.Size == null)
            return;
            
        // We cannot verify anything here, so just update it all the time.
        
        await _lobby.SendMessageAsync($"!mp set 0 0 {_lobby.Configuration.Size}");
    }

    private async Task EnsureRoomPassword()
    {
        if (_lobby.Configuration.Password == null)
            return;
        
        // We cannot verify anything here either.
        
        await _lobby.SendMessageAsync($"!mp password {_lobby.Configuration.Password}");
    }
}