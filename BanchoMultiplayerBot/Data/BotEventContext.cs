using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoMultiplayerBot.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Data;

public sealed class BotEventContext(IBehaviorData data, ILobby lobby)
{
    public ILobby Lobby { get; init; } = lobby;
    
    public IBehaviorData Data { get; init; } = data;

    public ICommandHandler CommandHandler => Lobby.BanchoConnection.CommandHandler;

    public string Channel => Lobby.MultiplayerLobby!.ChannelName;

    public void SendMessage(string contents)
    {
        if (Lobby.MultiplayerLobby == null)
        {
            Log.Error("BotEventContext: Attempt to send message while multiplayer lobby is null");
            throw new InvalidOperationException("Attempt to send message while multiplayer lobby is null.");
        }

        Lobby.BanchoConnection.MessageHandler.SendMessage(Channel, contents);
    }

    public async Task<bool> ExecuteCommandAsync<T>(IReadOnlyList<string>? args = null) where T : IBanchoCommand
    {
        return await CommandHandler.ExecuteAsync<T>(Channel, args);
    }
}