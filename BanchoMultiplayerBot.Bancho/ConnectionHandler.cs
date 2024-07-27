using BanchoMultiplayerBot.Bancho.Interfaces;
using Serilog;
using System.Net.Sockets;

namespace BanchoMultiplayerBot.Bancho
{
    internal class ConnectionHandler(TcpClient tcpClient, IMessageHandler messageHandler) : IConnectionHandler
    {
        public bool IsRunning { get; private set; } = false;

        private readonly TcpClient _tcpClient = tcpClient;
        private readonly IMessageHandler _messageHandler = messageHandler;

        private Task? _watchdogTask = null!;
        private bool _exitRequested = false;

        private DateTime _lastMessageReceived = DateTime.Now;

        public event Action? OnConnectionLost;

        public void Start()
        {
            Log.Verbose("ConnectionHandler: Starting watchdog task...");

            _exitRequested = false;
            _watchdogTask = Task.Run(ConnectionWatchdogTask);
        }

        public void Stop()
        {
            Log.Verbose("ConnectionHandler: Stopping watchdog task...");

            _exitRequested = true;

            if (_watchdogTask == null || _watchdogTask.Status != TaskStatus.Running)
            {
                Log.Warning("ConnectionHandler: Watchdog task is not running during Stop()");
                
                _watchdogTask = null;

                return;
            }

            _watchdogTask.Wait();
            _watchdogTask = null;
        }

        private async Task ConnectionWatchdogTask()
        {
            Log.Information("ConnectionHandler: Started watchdog task successfully");

            IsRunning = true;

            _messageHandler.OnMessageReceived += MessageHandler_OnMessageReceived;

            while (!_exitRequested)
            {
                await Task.Delay(1000);

                if (IsConnectionHealthy())
                {
                    continue;
                }

                Log.Error("ConnectionHandler: Detected lost connection!");

                // We don't want whatever callback where invoking to be blocking this task,
                // so invoke the event in another task.
                _ = Task.Run(() =>
                {
                    OnConnectionLost?.Invoke();
                });

                break;
            }

            Log.Verbose("ConnectionHandler: Watchdog task has stopped");

            _messageHandler.OnMessageReceived -= MessageHandler_OnMessageReceived;

            IsRunning = false;
        }

        private bool IsConnectionHealthy()
        {
            // First check if the TCP socket seems healthy
            if (!IsTcpConnectionAlive(_tcpClient))
            {
                Log.Error("ConnectionHandler: TCP socket unhealthy");

                return false;
            }

            // Additional fail-safe message check
            if (!((DateTime.Now - _lastMessageReceived).TotalMinutes > 5))
            {
                return true;
            }
            
            // We realistically should be receiving at least one message in 5 minutes during normal operation, so if we haven't
            // attempted to send a dummy message to see if the connection is still alive. We won't be signaling that the connection
            // is lost, since we're not sure if it's actually lost or not.

            Log.Warning("ConnectionHandler: No messages received in 5 minutes, sending dummy message to test connection");

            _messageHandler.SendMessage("dummy message", "dummy message");

            return true;

        }

        private void MessageHandler_OnMessageReceived(BanchoSharp.Interfaces.IPrivateIrcMessage obj)
        {
            _lastMessageReceived = DateTime.Now;
        }

        // See https://stackoverflow.com/a/6993334
        private static bool IsTcpConnectionAlive(TcpClient? client)
        {
            try
            {
                if (client != null && client.Client.Connected)
                {
                    if (!client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        return true;
                    }

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
}
