using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// Yet another band-aid, this time to detect inactivity in the lobby, since Bancho doesn't seem to notify
/// of lobby termination.
/// </summary>
public class InactivityDetectorBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private DateTime _lastLobbyMessage = DateTime.Now;

    private bool _hasNotifedError = false;
    private bool _isCheckingStatus = false;
    private DateTime _statusCheckTime = DateTime.Now;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        _lobby.Bot.Client.OnPrivateMessageReceived += OnMessageReceived;
    }
    
    private void OnMessageReceived(IPrivateIrcMessage msg)
    {
        // We'll just piggyback of the message received event to detect inactivity
        ValidateBotStatus();
        
        if (msg.Recipient != _lobby.Channel)
        {
            return;
        }

        _lastLobbyMessage = DateTime.Now;
    }

    private void ValidateBotStatus()
    {
        if (_isCheckingStatus)
        {
            // Make sure we give the "!mp settings" validation some time
            if ((DateTime.Now - _statusCheckTime).TotalSeconds < 30)
            {
                return;
            }
            
            if ((DateTime.Now - _lastLobbyMessage).TotalMinutes > 45)
            {
                // Still no message? Assume the lobby is closed
                if (!_hasNotifedError)
                {
                    Log.Error("Lobby {Channel} is dead, waiting for re-creation command.", _lobby.Configuration.Name);
                    _hasNotifedError = true;
                }
                
                _lobby.IsParted = true;
            }
            else
            {
                _isCheckingStatus = false;
            }
            
            return;
        }
        
        if ((DateTime.Now - _lastLobbyMessage).TotalMinutes < 45)
        {
            return;
        }
        
        Log.Warning("Lobby {Channel} has been inactive for 45 minutes, checking if lobby is still open", _lobby.Configuration.Name);

        _lobby.SendMessage("Assuming lobby is inactive due to inactivity, validating status...");
        _lobby.SendMessage("If this is artificially keeping the lobby open, and that's bad, please let me know.");
        _lobby.SendMessage("!mp settings");
        
        _isCheckingStatus = true;
        _statusCheckTime = DateTime.Now;

        try
        {
            if (_lobby.Bot.WebhookConfigured && _lobby.Bot.Configuration.WebhookNotifyLobbyTerminations == true)
            {
                _ = WebhookUtils.SendWebhookMessage(_lobby.Bot.Configuration.WebhookUrl!, "Channel Closed", $"Channel {_lobby.Configuration.Name} was parted.");
            }
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}