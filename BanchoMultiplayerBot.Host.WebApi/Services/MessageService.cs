using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

/// <summary>
/// This service will handle everything message related for the lobbies, such as sending and receiving messages,
/// and providing a list of previous messages for a specific lobby.
/// </summary>
public class MessageService(Bot bot, LobbyTrackerService lobbyTrackerService)
{
    /// <summary>
    /// Get a list of previous messages for a specific lobby id
    /// </summary>
    public IEnumerable<MessageModel> GetLobbyMessages(int lobbyId, int offset, int limit)
    {
        return lobbyTrackerService.GetLobbyInstance(lobbyId)?.Messages?.Skip(offset).Take(limit) ?? [];
    }
    
    /// <summary>
    /// Send a message to a specific lobby
    /// </summary>
    public void SendLobbyMessage(int lobbyId, string message)
    {
        var instance = lobbyTrackerService.GetLobbyInstance(lobbyId);
        
        if (instance?.Lobby.MultiplayerLobby == null)
        {
            throw new Exception("Lobby not found");
        }
        
        bot.BanchoConnection.MessageHandler.SendMessage(instance.Lobby.MultiplayerLobby.ChannelName, message);
    }
}