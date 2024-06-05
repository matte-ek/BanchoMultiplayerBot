namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    internal interface IConnectionHandler
    {
        public bool IsRunning { get; }

        public event Action? OnConnectionLost;

        public void Start();
        public void Stop();
    }
}
