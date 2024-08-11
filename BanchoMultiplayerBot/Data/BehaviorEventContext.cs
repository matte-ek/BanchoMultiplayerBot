using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot.Data;

public sealed class BehaviorEventContext(ILobby lobby, CancellationToken cancellationToken)
{
    public ILobby Lobby { get; init; } = lobby;
    
    public MultiplayerLobby MultiplayerLobby => Lobby.MultiplayerLobby!;
    
    public ICommandHandler CommandHandler => Lobby.BanchoConnection.CommandHandler;

    public ITimerProvider TimerProvider => Lobby.TimerProvider!;

    public IVoteProvider VoteProvider => Lobby.VoteProvider!;

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
            Log.Error("BehaviorEventContext: Attempt to send message while multiplayer lobby is null");
            throw new InvalidOperationException("Attempt to send message while multiplayer lobby is null.");
        }

        Lobby.BanchoConnection.MessageHandler.SendMessage(Channel, contents);
    }

    public async Task ExecuteCommandAsync<T>(IReadOnlyList<string>? args = null) where T : IBanchoCommand
    {
        await CommandHandler.ExecuteAsync<T>(Channel, args);
    }
    
    /// <summary>
    /// Get what string to use when passing a player as a parameter in tournament commands.
    /// This will make sure to prioritize player ID, or use player names if not available.
    /// </summary>
    public string GetPlayerIdentifier(string playerName)
    {
        int? playerId = MultiplayerLobby.Players.FirstOrDefault(x => x.Name == playerName)?.Id;

        return playerId == null ? playerName.Replace(' ', '_') : $"#{playerId}";
    }
}