using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho
{
    /// <summary>
    /// Quick wrapper for BanchoSharp's channel events, we do this so the user
    /// can easily subscribe to these events once, without having to worry about
    /// resubscribing every time the connection is lost internally.
    /// </summary>
    public class ChannelHandler(IBanchoConnection banchoConnection) : IChannelHandler
    {
        public event Action<IChatChannel>? OnChannelJoined;
        public event Action<string>? OnChannelJoinFailure;
        public event Action<IChatChannel>? OnChannelLeft;

        private IBanchoConnection _banchoConnection = banchoConnection;

        public void Start()
        {
            if (_banchoConnection.BanchoClient == null)
            {
                return;
            }

            _banchoConnection.BanchoClient.OnChannelJoined += BanchoOnChannelJoined;
            _banchoConnection.BanchoClient.OnChannelParted += BanchoOnChannelParted;
            _banchoConnection.BanchoClient.OnChannelJoinFailure += BanchoOnChannelJoinFailure;
        }

        public void Stop()
        {
            if (_banchoConnection.BanchoClient == null)
            {
                return;
            }

            _banchoConnection.BanchoClient.OnChannelJoined -= BanchoOnChannelJoined;
            _banchoConnection.BanchoClient.OnChannelParted -= BanchoOnChannelParted;
            _banchoConnection.BanchoClient.OnChannelJoinFailure -= BanchoOnChannelJoinFailure;
        }

        private void BanchoOnChannelParted(IChatChannel chatChannel)
        {
            OnChannelLeft?.Invoke(chatChannel);
        }

        private void BanchoOnChannelJoined(IChatChannel chatChannel)
        {
            OnChannelJoined?.Invoke(chatChannel);
        }

        private void BanchoOnChannelJoinFailure(string chatChannel)
        {
            OnChannelJoinFailure?.Invoke(chatChannel);
        }
    }
}
