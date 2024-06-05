using BanchoSharp;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IBanchoConnection
    {
        public BanchoClient? BanchoClient { get; }

        public IMessageHandler MessageHandler { get; }

        public bool IsConnected { get; }

        public Task ConnectAsync();

        public Task DisconnectAsync();
    }
}
