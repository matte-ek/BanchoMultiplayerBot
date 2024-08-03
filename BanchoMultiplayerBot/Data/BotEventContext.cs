﻿using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Data;

public sealed class BotEventContext(ILobby lobby, CancellationToken cancellationToken)
{
    public ILobby Lobby { get; init; } = lobby;
    
    public MultiplayerLobby MultiplayerLobby => Lobby.MultiplayerLobby!;
    
    public ICommandHandler CommandHandler => Lobby.BanchoConnection.CommandHandler;

    /// <summary>
    /// The channel name of the multiplayer lobby, formatted as "#mp_channel-id".
    /// </summary>
    public string Channel => Lobby.MultiplayerLobby!.ChannelName;

    /// <summary>
    /// A cancellation token that can be used to cancel the bot event.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = cancellationToken;

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