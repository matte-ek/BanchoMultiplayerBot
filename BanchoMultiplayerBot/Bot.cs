using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BanchoMultiplayerBot
{
    public class Bot(BanchoClientConfiguration banchoClientConfiguration)
    {
        public List<ILobby> Lobbies { get; } = [];
        
        public BanchoConnection BanchoConnection { get; } = new(banchoClientConfiguration);

        private CommandProcessor? _commandProcessor;
        
        public async Task StartAsync()
        {
            Log.Information("Bot starting up...");

            _commandProcessor ??= new CommandProcessor(this);
            
            BanchoConnection.OnReady += OnBanchoReady;
            
            _commandProcessor?.Start();

            await LoadLobbies();
            await BanchoConnection.ConnectAsync();
        }

        public async Task StopAsync()
        {
            Log.Information("Bot shutting down...");
            
            _commandProcessor?.Stop();
            
            BanchoConnection.OnReady -= OnBanchoReady;

            foreach (var lobby in Lobbies)
            {
                await lobby.Dispose();
            }

            await BanchoConnection.DisconnectAsync();
        }
        
        private async Task LoadLobbies()
        {
            await using var context = new BotDbContext();
            
            var lobbyConfigurations = await context.LobbyConfigurations.ToListAsync();
            
            foreach (var lobbyConfiguration in lobbyConfigurations)
            {
                if (Lobbies.Any(x => x.LobbyConfigurationId == lobbyConfiguration.Id))
                {
                    continue;
                }
                
                Lobbies.Add(new Lobby(BanchoConnection, lobbyConfiguration.Id));
                
                Log.Information("Bot: Loaded lobby configuration {LobbyConfigurationId}", lobbyConfiguration.Id);
            }
        }
        
        private async void OnBanchoReady()
        {
            foreach (var lobby in Lobbies)
            {
                Log.Information("Bot: Starting lobby {LobbyConfigurationId}...", lobby.LobbyConfigurationId);
                
                await lobby.ConnectAsync();

                var attempts = 0;
                while (!lobby.IsReady)
                {
                    if (attempts++ > 20)
                    {
                        Log.Error("Bot: Lobby {LobbyIndex} did not become ready after 10 seconds.", lobby.LobbyConfigurationId);
                        break;
                    }
                    
                    await Task.Delay(500);
                }
            }
        }
    }
}
