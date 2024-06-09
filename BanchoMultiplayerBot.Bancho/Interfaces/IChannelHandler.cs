using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IChannelHandler
    {
        public event Action<IChatChannel>? OnChannelJoined;
        public event Action<string>? OnChannelJoinFailure;
        public event Action<IChatChannel>? OnChannelLeft;

        public void Start();
        public void Stop();
    }
}
