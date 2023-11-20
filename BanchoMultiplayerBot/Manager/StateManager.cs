using System.Text.Json;
using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Data;
using Serilog;

namespace BanchoMultiplayerBot.Manager;

public class StateManager
{
    private const string StateFile = "lobby_states.json";
    private readonly Bot _bot;

    public StateManager(Bot bot)
    {
        _bot = bot;
    }

    /// <summary>
    /// Saves all the current lobbies channels and queue and their configuration. When everything is saved, the bot may pick up
    /// where it left via AutoRecoverExistingLobbies()
    /// </summary>
    public void SaveState()
    {
        _bot.ConfigurationManager.SaveConfiguration();

        if (!_bot.Lobbies.Any())
            return;

        List<LobbyState> lobbyStates = new();

        foreach (var lobby in _bot.Lobbies)
        {
            if (lobby.IsParted)
                continue;
            
            string? queue = null;
            List<PlaytimeRecord> playtimeRecords = new();

            try
            {
                // Special case for AutoHostRotateBehaviour, this should be done differently if any more of these cases come up.
                // This will save the queue, so that also gets recovered successfully.
                if (lobby.Behaviours.Find(x => x.GetType() == typeof(AutoHostRotateBehaviour)) is AutoHostRotateBehaviour autoHostRotateBehaviour)
                {
                    queue = string.Join(',', autoHostRotateBehaviour.Queue);
                }

                playtimeRecords.AddRange(lobby.MultiplayerLobby.Players.Select(player => new PlaytimeRecord() { Name = player.Name, JoinTime = player.JoinTime.ToBinary()}));
            }
            catch (Exception)
            {
                // ignored
            }

            lobbyStates.Add(new LobbyState()
            {
                Channel = lobby.MultiplayerLobby.ChannelName,
                Name = lobby.Configuration.Name,
                Queue = queue,
                PlayerPlaytime = playtimeRecords.ToArray()
            });
        }

        File.WriteAllText(StateFile, JsonSerializer.Serialize(lobbyStates));

        Log.Information($"Saved bot state successfully ({lobbyStates.Count} lobbies)");
    }

    /// <summary>
    /// Attempts to rejoin lobbies that were previously created. Allows the bot to fully recover from
    /// restarts, network issues, bancho restarts and whatnot. If the previous lobbies were not found, 
    /// they will be created.
    /// </summary>
    public bool LoadState()
    {
        // All previous lobby information is stored in lobby_states.json
        if (!File.Exists(StateFile))
            return false;

        // Load and parse the JSON file
        var reader = File.OpenRead(StateFile);
        var lobbyStates = JsonSerializer.Deserialize<List<LobbyState>>(reader);

        reader.Close();
        reader.Dispose();

        if (lobbyStates == null)
            return false;

        Log.Information("Recovering existing lobbies...");

        _bot.Client.OnChannelJoinFailure += async name =>
        {
            // Attempt to create a new lobby instead.
            var lobbyName = lobbyStates.FirstOrDefault(x => x.Channel == name);
            var lobbyConfig = _bot.Configuration.LobbyConfigurations?.FirstOrDefault(x => x.Name == lobbyName?.Name);

            if (lobbyConfig == null)
            {
                return;
            }

            Log.Warning($"Failed to find lobby by name {lobbyName?.Name}, creating new one instead.");

            var failedLobby = _bot.Lobbies.FirstOrDefault(x => x.Configuration.Name ==  lobbyName?.Name);
            if (failedLobby != null)
            {
                _bot.Lobbies.Remove(failedLobby);
            }

            await _bot.CreateLobbyAsync(lobbyConfig);
        };

        var lobbyIndex = 0;
        foreach (var lobby in lobbyStates.OrderBy(x => x.Name).ToList())
        {
            // Attempt to find the correct configuration within our lobby configurations.
            var config = _bot.Configuration.LobbyConfigurations?.FirstOrDefault(x => x.Name == lobby.Name);

            if (config == null)
            {
                // Not sure how I want the bot to behave in this case yet, return is intentional. 
                Log.Error($"Failed to find configuration for lobby during recovery.");

                return false;
            }
            
            config.PreviousQueue = lobby.Queue;
            config.PlayerPlaytime = lobby.PlayerPlaytime;

            Task.Delay(lobbyIndex * 1000).ContinueWith(async _ => { await _bot.AddLobbyAsync(lobby.Channel, config); });

            lobbyIndex++;
        }

        return true;
    }

}