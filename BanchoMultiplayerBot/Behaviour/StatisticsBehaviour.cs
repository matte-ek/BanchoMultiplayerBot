namespace BanchoMultiplayerBot.Behaviour;

/// <summary>
/// Behaviour to provide some anonymous statistics for the bot, such as "games played",
/// "games aborted", "player count", "unique amount of players last hour".
/// </summary>
public class StatisticsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    private readonly List<Tuple<string, DateTime>> _players = new();
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.MultiplayerLobby.OnMatchFinished += () =>
        {
            _lobby.Bot.RuntimeInfo.Statistics.GamesPlayed.WithLabels(_lobby.LobbyLabel).Inc();
        };

        _lobby.MultiplayerLobby.OnMatchAborted += () =>
        {
            _lobby.Bot.RuntimeInfo.Statistics.GamesAborted.WithLabels(_lobby.LobbyLabel).Inc();
        }; 
        
        _lobby.MultiplayerLobby.OnPlayerJoined += (e) =>
        {
            if (_players.All(x => x.Item1 != e.Name))
            {
                _players.Add(new Tuple<string, DateTime>(e.Name, DateTime.Now));
            }
            
            _players.RemoveAll(x => DateTime.Now > x.Item2.AddHours(1));
            
            _lobby.Bot.RuntimeInfo.Statistics.Players.WithLabels(_lobby.LobbyLabel).Set(_lobby.MultiplayerLobby.Players.Count);
            _lobby.Bot.RuntimeInfo.Statistics.UniquePlayers.WithLabels(_lobby.LobbyLabel).Set(_players.Count);
        }; 
        
        _lobby.MultiplayerLobby.OnPlayerDisconnected += (e) =>
        {
            _lobby.Bot.RuntimeInfo.Statistics.Players.WithLabels(_lobby.LobbyLabel).Set(_lobby.MultiplayerLobby.Players.Count);
        }; 
    }
}