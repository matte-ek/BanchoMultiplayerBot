using System.Net.Sockets;
using BanchoMultiplayerBot.Utilities;
using BanchoSharp;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Manager;

public class ConnectionManager
{
    private readonly Bot _bot;
    private bool _exitRequested;

    private DateTime _lastMessageTime = DateTime.Now;

    public ConnectionManager(Bot bot)
    {
        this._bot = bot;
    }

    public void Start()
    {
        _exitRequested = false;
        _lastMessageTime = DateTime.Now;

        _bot.Client.OnPrivateMessageReceived += OnPrivateMessageReceived;

        Task.Run(ConnectionWatchdogTask);
    }

    public void Stop()
    {
        _exitRequested = true;
    }

    private void OnPrivateMessageReceived(IPrivateIrcMessage msg)
    {        
        try
        {
            if (msg.Recipient == "#osu" && 
                msg.IsBanchoBotMessage &&
                msg.Content.StartsWith("Bancho will be restarting for maintenance in 1 minute."))
            {
                if (_bot.WebhookConfigured && _bot.Configuration.WebhookNotifyConnectionErrors == true)
                {
                    _ = WebhookUtils.SendWebhookMessage(_bot.Configuration.WebhookUrl!, "Bancho Restart", $"Bancho is queued to restart within a minute.");
                }
            
                _bot.AnnouncementManager.SendAnnouncementMessage("Bancho is about to restart, the lobby should be automatically re-created in few minutes after Bancho is restarted.");
                _bot.AnnouncementManager.SendAnnouncementMessage("Try searching for the lobby if you cannot find it in the list, thanks for playing!");
            }
        }
        catch (Exception e)
        {
            Log.Error($"{e}");
        }
    }

    /// <summary>
    /// Task to continuously monitor the TCP connection to Bancho, and in case of an network issue
    /// automatically attempt to reconnect to Bancho and restore the bot.
    /// </summary>
    private async Task ConnectionWatchdogTask()
    {
        int connectionAttempts = 0;

        _exitRequested = false;

        _bot.Client.OnPrivateMessageReceived += _ => 
        {
            _lastMessageTime = DateTime.Now;
        };

        _bot.Client.OnAuthenticated += () => 
        {
            _exitRequested = false;
        };

        while (!_exitRequested)
        {
            if (!IsTcpConnectionAlive(_bot.Client.TcpClient))
            {
                Log.Error("[!] DETECTED CONNECTION ERROR!");

                if (_bot.WebhookConfigured && _bot.Configuration.WebhookNotifyConnectionErrors == true)
                {
                    _ = WebhookUtils.SendWebhookMessage(_bot.Configuration.WebhookUrl!, "Connection Error", $"Detected connection error to osu!bancho");
                }

                _bot.RuntimeInfo.HadNetworkConnectionIssue = true;
                _bot.RuntimeInfo.LastConnectionIssueTime = DateTime.Now;
                
                _bot.RuntimeInfo.Statistics.IsConnected.Set(0);

                _bot.StateManager.SaveState();

                while (connectionAttempts <= 20 && !IsTcpConnectionAlive(_bot.Client.TcpClient))
                {
                    connectionAttempts++;

                    Log.Information("Attempting to reconnect in 30 seconds");

                    await Task.Delay(30000);

                    _bot.Client.Dispose();
                    _bot.Lobbies.Clear();

                    _bot.Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(_bot.Configuration.Username, _bot.Configuration.Password), LogLevel.Trace));

                    _ = Task.Run(_bot.RunAsync);

                    await Task.Delay(10000);
                }

                break;
            }

            try
            {
                // This is an additional fail-safe for the connection state, by checking the last time we received a message,
                // so if we haven't received a message for 5 minutes, then write a message to test the connection.
                // I feel like 5 minutes is a pretty safe bet for now.
                if ((DateTime.Now - _lastMessageTime).TotalSeconds > 300)
                {
                    _lastMessageTime = DateTime.Now;

                    _bot.SendMessage("BanchoBot", $"connection check: {DateTime.Now}");

                    Log.Warning("No message for 5 minutes, testing connection by sending a message to BanchoBot.");
                }
            }
            catch (Exception)
            {
                // ignored
            }

            await Task.Delay(1000);
        }

        if (!_exitRequested)
        {
            Log.Information(_bot.Client.IsConnected
                ? "Successfully re-connected to Bancho!"
                : "Failed to restart the bot after 20 attempts.");
        }
    }

    // See https://stackoverflow.com/a/6993334
    private static bool IsTcpConnectionAlive(TcpClient? client)
    {
        try
        {
            if (client != null && client.Client.Connected)
            {
                // Detect if client disconnected
                if (!client.Client.Poll(0, SelectMode.SelectRead))
                    return true;

                byte[] buff = new byte[1];

                return client.Client.Receive(buff, SocketFlags.Peek) != 0;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

}