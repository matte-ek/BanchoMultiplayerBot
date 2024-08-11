using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BanchoMultiplayerBot.Providers;

public class VoteProvider(ILobby lobby) : IVoteProvider
{
    private readonly ILobby _lobby = lobby;
    private readonly List<IVote> _votes = [];

    public IVote FindOrCreateVote(string name, string description)
    {
        var vote = _votes.FirstOrDefault(x => x.Name == name);

        if (vote == null)
        {
            vote = new Vote(name, description, _lobby);
            _votes.Add(vote);
        }

        return vote;
    }

    public async Task Start()
    {
        await LoadVotes();
    }

    public async Task Stop()
    {
        await SaveVotes();
    }
    
    private async Task LoadVotes()
    {
        await using var context = new BotDbContext();

        var votes = await context.LobbyVotes
            .Where(x => x.LobbyId == _lobby.LobbyConfigurationId)
            .ToListAsync();

        foreach (var vote in votes)
        {
            var newVote = new Vote(vote.Name, vote.Description, _lobby)
            {
                IsActive = vote.IsActive,
                Votes = vote.Votes,
                StartTime = vote.StartTime
            };
            
            if ((DateTime.UtcNow - newVote.StartTime).TotalSeconds > 120 && newVote.IsActive)
            {
                newVote.IsActive = false;
                Log.Warning("VoteProvider: Vote {VoteName} has been active for over 2 minutes, aborting", newVote.Name);
            }
            
            _votes.Add(newVote);
        }
        
        Log.Verbose("VoteProvider: Loaded {VoteCount} vote(s) from database", _votes.Count);
    }
    
    private async Task SaveVotes()
    {
        await using var context = new BotDbContext();

        foreach (var vote in _votes)
        {
            var existingVote = await context.LobbyVotes.FirstOrDefaultAsync(x => x.LobbyId == _lobby.LobbyConfigurationId && x.Name == vote.Name);

            if (existingVote == null)
            {
                existingVote = new LobbyVote
                {
                    LobbyId = _lobby.LobbyConfigurationId,
                    Name = vote.Name,
                    Description = vote.Description,
                    IsActive = vote.IsActive,
                    Votes = vote.Votes,
                    StartTime = vote.StartTime
                };
                
                context.LobbyVotes.Add(existingVote);
            }

            existingVote.Description = vote.Description;
            existingVote.IsActive = vote.IsActive;
            existingVote.Votes = vote.Votes;
            existingVote.StartTime = vote.StartTime;
        }
        
        await context.SaveChangesAsync();
    }
}