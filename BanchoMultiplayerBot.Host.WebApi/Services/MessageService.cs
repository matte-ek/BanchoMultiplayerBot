using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;
using Microsoft.AspNetCore.Authorization;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

/// <summary>
/// This service will handle everything message related for the lobbies, such as sending and receiving messages,
/// and providing a list of previous messages for a specific lobby.
/// </summary>
public class MessageService(Bot bot, LobbyTrackerService lobbyTrackerService)
{
    /// <summary>
    /// Get a list of previous messages for a specific lobby id.
    /// Returns the most recent <paramref name="limit"/> messages, with
    /// <paramref name="offset"/> paging further back into history, while keeping
    /// chronological (oldest -> newest) order.
    /// </summary>
    public IEnumerable<MessageModel> GetLobbyMessages(int lobbyId, int offset, int limit)
    {
        var messages = lobbyTrackerService.GetLobbyInstance(lobbyId)?.Messages;

        if (messages == null || limit <= 0)
        {
            return [];
        }

        // The buffer is ordered oldest -> newest, so the tail is the newest.
        // Snapshot the slice with ToList() to avoid enumerating the live list
        // while it may be mutated by the message handler on another thread.
        var end = Math.Max(0, messages.Count - offset);
        var start = Math.Max(0, end - limit);

        return messages.Skip(start).Take(end - start).ToList();
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