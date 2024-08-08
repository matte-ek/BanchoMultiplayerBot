namespace BanchoMultiplayerBot.Interfaces;

public interface IVote
{
    public string Name { get; }
    
    public bool IsActive { get; set; }
}