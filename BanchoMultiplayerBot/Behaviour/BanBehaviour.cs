using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Behaviour;

public class BanBehaviour : IBotBehaviour
{
    private Lobby _lobby = null!;
    
    public void Setup(Lobby lobby)
    {
        _lobby = lobby;
        
        _lobby.OnAdminMessage += OnAdminMessage;
        _lobby.MultiplayerLobby.OnPlayerJoined += OnPlayerJoined;
    }
    
    private void OnAdminMessage(IPrivateIrcMessage e)
    {
        if (e.Content.StartsWith("!ban "))
        {
            try
            {
                var nameSplit = e.Content.Split("!ban ")[1];

                if (_lobby.Bot.Configuration.BannedPlayers == null)
                {
                    _lobby.Bot.Configuration.BannedPlayers = new string[] { nameSplit };
                }
                else
                {
                    if (_lobby.Bot.Configuration.BannedPlayers.ToList().Contains(nameSplit))
                    {
                        // Player already exists.
                        return;
                    }

                    var banList = _lobby.Bot.Configuration.BannedPlayers.ToList();
                    
                    banList.Add(nameSplit);

                    _lobby.Bot.Configuration.BannedPlayers = banList.ToArray();
                }
            }
            catch (Exception)
            {
                // ignored.
            }
        }
        
        if (e.Content.StartsWith("!banmapset "))
        {
            try
            {
                var beatmapSetId = int.Parse(e.Content.Split("!banmapset ")[1]);

                if (_lobby.Bot.Configuration.BannedBeatmaps == null)
                {
                    _lobby.Bot.Configuration.BannedBeatmaps = new int[] { beatmapSetId };
                }
                else
                {
                    if (_lobby.Bot.Configuration.BannedBeatmaps.ToList().Contains(beatmapSetId))
                    {
                        // Player already exists.
                        return;
                    }

                    var banList = _lobby.Bot.Configuration.BannedBeatmaps.ToList();
                    
                    banList.Add(beatmapSetId);

                    _lobby.Bot.Configuration.BannedBeatmaps = banList.ToArray();
                }
            }
            catch (Exception)
            {
                // ignored.
            }
        }
    }

    private void OnPlayerJoined(MultiplayerPlayer player)
    {
        var name = player.Name;
        var botConfiguration = _lobby.Bot.Configuration;

        if (botConfiguration.BannedPlayers == null)
            return;

        try
        {
            if (botConfiguration.BannedPlayers.ToList().Contains(name))
            {
                _lobby.SendMessage($"!mp ban {name.Replace(' ','_')}");
            }
        }
        catch (Exception)
        {
            // ignored.
        }
    }
}