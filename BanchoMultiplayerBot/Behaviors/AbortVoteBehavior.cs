using BanchoMultiplayerBot.Attributes;
using BanchoMultiplayerBot.Bancho.Commands;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Interfaces;

namespace BanchoMultiplayerBot.Behaviors;

public class AbortVoteBehavior(BotEventContext context) : IBehavior
{
    [BanchoEvent(BanchoEventType.MatchAborted)]
    public void OnMatchAborted()
    {
        context.Lobby.VoteProvider?.FindOrCreateVote("AbortVote").Abort();
    }
    
    [BanchoEvent(BanchoEventType.MatchFinished)]
    public void OnMatchFinished()
    {
        context.Lobby.VoteProvider?.FindOrCreateVote("AbortVote").Abort();
    }
    
    [BotEvent(BotEventType.CommandExecuted, "Abort")]
    public async Task OnAbortCommandExecuted(CommandEventContext commandEventContext)
    {
        if (commandEventContext.Player == null)
        {
            return;
        }
        
        var abortVote = context.Lobby.VoteProvider!.FindOrCreateVote("AbortVote");
        if (abortVote.PlayerVote(commandEventContext.Player))
        {
            await context.ExecuteCommandAsync<MatchAbortCommand>();
        }
    }
}