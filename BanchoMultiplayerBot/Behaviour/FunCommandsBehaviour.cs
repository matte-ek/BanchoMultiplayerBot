using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoSharp.EventArgs;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Behaviour;

public class FunCommandsBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;

    private DatabaseContext? Database => _lobby.Bot.Database;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnUserMessage += OnUserMessage;

        _lobby.MultiplayerLobby.OnPlayerDisconnected += OnPlayerDisconnected;
    }

    private void OnPlayerDisconnected(PlayerDisconnectedEventArgs playerDisconnectedEventArgs)
    {
        var player = playerDisconnectedEventArgs.Player;
        
        if (Database == null)
            return;
        if (player.Id == null)
            return;

        try
        {
            var dbPlayer = Database.Players.SingleOrDefault(x => x.Id == player.Id);

            if (dbPlayer == null)
            {
                Database.Players.Add(new Player
                {
                    Id = player.Id.Value
                });
            
                dbPlayer = Database.Players.SingleOrDefault(x => x.Id == player.Id);

                if (dbPlayer == null)
                {
                    Log.Error($"Unable to create player {player.Id} in database.");
                    return;
                }
            }
            
            var currentSessionTime = DateTime.Now - player.JoinTime;

            dbPlayer.Name = player.Name;
            dbPlayer.TotalPlaytime += currentSessionTime.Seconds;

            Database.SaveChanges();
        }
        catch (Exception e)
        {
            // ignored
        }
    }

    private void OnUserMessage(IPrivateIrcMessage e)
    {
        try
        {
            var player = _lobby.MultiplayerLobby.Players.FirstOrDefault(x => x.Name == e.Sender);

            if (player is null)
            {
                return;
            }
            
            if (e.Content.Equals("!playtime"))
            {
                var time = DateTime.Now - player.JoinTime;
                
                _lobby.SendMessage($"{e.Sender} has been here for {time:h' hours 'm' minutes 's' seconds'}");
            }
        }
        catch (Exception exception)
        {
            // ignored
        }
    }
}