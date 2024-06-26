using BanchoMultiplayerBot.Bancho;
using BanchoSharp.Multiplayer;
using System.Text.Json.Nodes;
using BanchoMultiplayerBot.Configuration;

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
        /// The configuration of the lobby, such as the name, mode, and size
        /// </summary>
        public int LobbyConfigurationId { get; set; }
        
        /// <summary>
        /// Runtime data that can be used by behaviors, such as the current queue. Stuff that isn't static, unlike the configuration
        /// </summary>
        public JsonObject RuntimeData { get; }
        
        /// <summary>
        /// Attempts to connect to an existing channel, or creates a new one if none is provided
        /// </summary>
        public Task ConnectAsync(string? existingChannel);
        
        /// <summary>
        /// Initializes the lobby and sets up event handlers, called once during application lifetime
        /// </summary>
        public void Setup();
        
        /// <summary>
        /// Disposes of the lobby and removes event handlers, called once during application lifetime
        /// </summary>
        public void Dispose();

        public T? GetBehavior<T>();
    }
}
