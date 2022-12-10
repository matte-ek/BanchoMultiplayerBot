namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// A behaviour is the "module" that registers events and does whatever bot stuff it has to do.
/// They are split up into separate parts, for example `LobbyManagerBehaviour` will manage room details
/// (such as name, password, size etc), `AutoHostRotateBehaviour` will run the host rotate and so forth.  
/// </summary>
public interface IBotBehaviour
{
    
    void Setup(Lobby lobby);

}