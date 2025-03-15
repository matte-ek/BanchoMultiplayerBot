using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    /// <summary>
    /// Handler for managing channels that we are connected to.
    /// </summary>
    public interface IChannelHandler
    {
        /// <summary>
        /// Invoked whenever the bot has created a _new_ multiplayer lobby.
        /// </summary>
        public event Action<IMultiplayerLobby>? OnLobbyCreated;
        
        /// <summary>
        /// Invoked whenever the bot has joined a channel.
        /// </summary>
        public event Action<IChatChannel>? OnChannelJoined;
        
        /// <summary>
        /// Invoked whenever the bot has failed to join a channel, which can happen if the channel does not exist for example.
        /// </summary>
        public event Action<string>? OnChannelJoinFailure;
        
        /// <summary>
        /// Invoked whenever the bot has left a channel.
        /// </summary>
        public event Action<IChatChannel>? OnChannelLeft;

        /// <summary>
        /// There is two types of ids within bancho. The first obvious one is the one in the channel name,
        /// i.e. "#mp_1234567". The second one is a runtime id, which is an incremental number that is assigned
        /// to the channel when it is created, however this is stateful and will be reset when Bancho restarts.
        /// This method will return the runtime id of a channel, if we have it.
        /// </summary>
        /// <param name="channelName">The channel name, i.e. "#mp_1234567"</param>
        /// <returns>The runtime channel id, if known.</returns>
        public int? GetChannelRuntimeId(string channelName);
        
        internal void Start();
        internal void Stop();
    }
}
