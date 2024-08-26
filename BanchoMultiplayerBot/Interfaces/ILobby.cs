using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database.Models;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Interfaces
{
    public interface ILobby
    {
        /// <summary>
        /// The main bot instance
        /// </summary>
        public Bot Bot { get; init; }
        
        /// <summary>
        /// The bancho connection instance
        /// </summary>
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
        /// The current state of the lobby
        /// </summary>
        public LobbyHealth Health { get; set; }
        
        /// <summary>
        /// Event dispatcher for behavior events
        /// </summary>
        public IBehaviorEventProcessor? BehaviorEventProcessor { get; }
        
        /// <summary>
        /// Utility for managing timers within behaviors
        /// </summary>
        public ITimerProvider? TimerProvider { get; }
        
        /// <summary>
        /// Utility for managing votes within behaviors
        /// </summary>
        public IVoteProvider? VoteProvider { get; }

        /// <summary>
        /// Whenever the lobby has started in a new channel
        /// </summary>
        public event Action? OnStarted;
        
        /// <summary>
        /// Whenever the lobby has stopped in a previous channel
        /// </summary>
        public event Action? OnStopped;
        
        /// <summary>
        /// Attempts to connect to an existing channel, or creates a new one if none is provided
        /// </summary>
        public Task ConnectAsync();

        /// <summary>
        /// Restarts the lobby instance, reloads behaviors and such
        /// </summary>
        public Task RefreshAsync();
        
        /// <summary>
        /// Disposes of the lobby and removes event handlers, called once during application lifetime
        /// </summary>
        public Task Dispose();

        /// <summary>
        /// Gets the lobby configuration from the database
        /// </summary>
        public Task<LobbyConfiguration> GetLobbyConfiguration();
    }
}
