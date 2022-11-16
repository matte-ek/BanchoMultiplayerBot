namespace BanchoMultiplayerBot.Behaviour;

public class AutoStartBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
    }
    
    
}