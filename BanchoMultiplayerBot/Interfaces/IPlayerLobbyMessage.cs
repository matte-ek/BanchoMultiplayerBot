using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Interfaces
{
    /// <summary>
    /// A player message that was sent in a lobby
    /// </summary>
    public interface IPlayerLobbyMessage : IPlayerMessage
    {
        /// <summary>
        /// The lobby of which the message was sent
        /// </summary>
        public ILobby Lobby { get; }

        /// <summary>
        /// The lobby player instance of the player who sent the message
        /// </summary>
        public MultiplayerPlayer Player { get; }
    }
}
