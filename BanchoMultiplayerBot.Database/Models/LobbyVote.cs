﻿namespace BanchoMultiplayerBot.Database.Models;

public class LobbyVote
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int LobbyId { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime PassTime { get; set; }

    public List<string> Votes { get; set; } = [];
}