using BanchoSharp;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IBanchoConnection
    {
        public IBanchoClient? BanchoClient { get; }

        public IMessageHandler MessageHandler { get; }

        public bool IsConnected { get; }

        public Task StartAsync();
        
        public Task StopAsync();
    }
}
