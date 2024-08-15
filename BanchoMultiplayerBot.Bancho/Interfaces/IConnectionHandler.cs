namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    internal interface IConnectionHandler
    {
        public event Action? OnConnectionLost;
        
        /// <summary>
        /// Whether the watchdog task is running or not.
        /// </summary>
        public bool IsRunning { get; }
        
        public void Start();
        
        public void Stop();
    }
}
