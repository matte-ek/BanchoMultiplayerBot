using BanchoMultiplayerBot.Bancho;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Interfaces
{
    public interface ILobby
    {
        public BanchoConnection BanchoConnection { get; init; }
        
        /// <summary>
        /// The BanchoSharp multiplayer lobby instance
        /// </summary>
        public MultiplayerLobby? MultiplayerLobby { get; }

        /// <summary>
        /// Database id of the lobby configuration
        /// </summary>
        public int LobbyConfigurationId { get; set; }
        
        /// <summary>
        /// Event dispatcher for behavior events
        /// </summary>
        public IBehaviorEventDispatcher? BehaviorEventDispatcher { get; }
        
        /// <summary>
        /// Attempts to connect to an existing channel, or creates a new one if none is provided
        /// </summary>
        public Task ConnectAsync(string? existingChannel);
        
        /// <summary>
        /// Disposes of the lobby and removes event handlers, called once during application lifetime
        /// </summary>
        public Task Dispose();
    }
}
