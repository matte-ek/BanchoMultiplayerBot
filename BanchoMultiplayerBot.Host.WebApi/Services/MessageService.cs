using BanchoMultiplayerBot.Host.WebApi.DataTransferObjects;

namespace BanchoMultiplayerBot.Host.WebApi.Services;

public class MessageService(Bot bot, LobbyTrackerService lobbyTrackerService)
{
    public IEnumerable<MessageModel> GetLobbyMessages(int lobbyId, int offset, int limit)
    {
        return lobbyTrackerService.GetLobbyInstance(lobbyId)?.Messages?.Skip(offset).Take(limit) ?? [];
    }
    
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