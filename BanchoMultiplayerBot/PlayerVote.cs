﻿using BanchoSharp.Multiplayer;
using Serilog;

namespace BanchoMultiplayerBot;

/// <summary>
/// Utility to easily create a player vote
/// </summary>
public class PlayerVote
{
    public string Question { get; }
    public Lobby Lobby { get; }
    
    public List<MultiplayerPlayer> Votes { get; } = new();
    
    public PlayerVote(Lobby lobby, string question)
    {
        Lobby = lobby;
        Question = question;

        Lobby.MultiplayerLobby.OnPlayerDisconnected += disconnectArgs =>
        {
            Votes.Remove(disconnectArgs.Player);
        };
    }

    public bool Vote(MultiplayerPlayer player)
    {
        int requiredVotes = Math.Max(Lobby.MultiplayerLobby.Players.Count / 2 + 1, 1);

        if (Votes.Contains(player))
        {
            return false;
        }
        
        Votes.Add(player);
        
        Lobby.SendMessage($"{Question} ({Votes.Count}/{requiredVotes})");

        if (Votes.Count < requiredVotes) 
            return false;

        Log.Information($"Passed vote {Question} with ({Votes.Count}/{requiredVotes})");

        Reset();
        
        return true;
    }

    public void Reset()
    {
        Votes.Clear();
    }
    
}