using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Bancho
{
    /// <summary>
    /// Quick wrapper for BanchoSharp's channel events, we do this so the user
    /// can easily subscribe to these events once, without having to worry about
    /// resubscribing every time the connection is lost internally.
    /// </summary>
    public class ChannelHandler(IBanchoConnection banchoConnection) : IChannelHandler
    {
        public event Action<IMultiplayerLobby>? OnLobbyCreated;
        public event Action<IChatChannel>? OnChannelJoined;
        public event Action<string>? OnChannelJoinFailure;
        public event Action<IChatChannel>? OnChannelLeft;
        
        private readonly Dictionary<string, int> _channelIds = new();

        public int? GetChannelRuntimeId(string channelName)
        {
            if (_channelIds.TryGetValue(channelName, out var channelId))
            {
                return channelId;
            }

            return null;
        }

        public void Start()
        {
            if (banchoConnection.BanchoClient == null)
            {
                return;
            }

            banchoConnection.BanchoClient.BanchoBotEvents.OnTournamentLobbyCreated += BanchoOnLobbyCreated;
            banchoConnection.BanchoClient.OnChannelJoined += BanchoOnChannelJoined;
            banchoConnection.BanchoClient.OnChannelParted += BanchoOnChannelParted;
            banchoConnection.BanchoClient.OnChannelJoinFailure += BanchoOnChannelJoinFailure;
            banchoConnection.BanchoClient.OnMessageReceived += OnMessageReceived;
        }

        public void Stop()
        {
            if (banchoConnection.BanchoClient == null)
            {
                return;
            }

            banchoConnection.BanchoClient.BanchoBotEvents.OnTournamentLobbyCreated -= BanchoOnLobbyCreated;
            banchoConnection.BanchoClient.OnChannelJoined -= BanchoOnChannelJoined;
            banchoConnection.BanchoClient.OnChannelParted -= BanchoOnChannelParted;
            banchoConnection.BanchoClient.OnChannelJoinFailure -= BanchoOnChannelJoinFailure;
            banchoConnection.BanchoClient.OnMessageReceived -= OnMessageReceived;
        }
        
        private void BanchoOnLobbyCreated(IMultiplayerLobby lobby)
        {
            OnLobbyCreated?.Invoke(lobby);
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
        
        private void OnMessageReceived(IIrcMessage msg)
        {
            if (msg.Command != "332") // RPL_TOPIC
            {
                return;
            }

            try
            {
                var multiplayerId = msg.RawMessage[msg.RawMessage.IndexOf("#mp_", StringComparison.Ordinal)..msg.RawMessage.IndexOf(" :", StringComparison.Ordinal)];
                var numberId = msg.RawMessage[(msg.RawMessage.LastIndexOf("#", StringComparison.Ordinal) + 1)..];
    
                _channelIds.TryAdd(multiplayerId, int.Parse(numberId));
            }
            catch (Exception e)
            {
                Log.Error("ChannelHandler: Error while parsing channel ID from message {msg.RawMessage}, {e.Message}", msg.RawMessage, e);
            }
        }
    }
}
