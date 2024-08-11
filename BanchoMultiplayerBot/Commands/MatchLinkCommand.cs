using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Commands;

public class MatchLinkCommand : IPlayerCommand
{
    public string Command => "MatchLink";

    public List<string>? Aliases => ["mplink"];

    public bool AllowGlobal => false;

    public bool Administrator => false;

    public int MinimumArguments => 0;

    public string? Usage => null;

    public Task ExecuteAsync(CommandEventContext context)
    {
        if (context.Lobby != null)
        {
            context.Reply($"Match history available [https://osu.ppy.sh/community/matches/{context.Lobby.MultiplayerLobby?.ChannelName[4..]} here.]");
        }

        return Task.CompletedTask;
    }
}