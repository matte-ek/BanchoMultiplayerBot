﻿using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Interfaces;
using BanchoMultiplayerBot.Notifications;
using BanchoMultiplayerBot.Osu;
using BanchoMultiplayerBot.Utilities;
using Microsoft.EntityFrameworkCore;
using osu.NET;
using osu.NET.Authorization;
using Serilog;

namespace BanchoMultiplayerBot
{
    public class Bot(IBotConfiguration botConfiguration) : IAsyncDisposable
    {
        public List<ILobby> Lobbies { get; } = [];
        
        public BanchoConnection BanchoConnection { get; } = new(botConfiguration.BanchoClientConfiguration);

        public OsuApiClient OsuApiClient { get; } = new(new OsuClientAccessTokenProvider(botConfiguration.OsuApiClientId, botConfiguration.OsuApiClientSecret), new OsuApiClientOptions(), null!);

        public PerformancePointCalculator? PerformancePointCalculator { get; } = new();

        public NotificationManager NotificationManager { get; } = new(botConfiguration);
        
        public event Action<ILobby>? OnLobbyCreated;
        public event Action<ILobby>? OnLobbyRemoved;
        
        private CommandProcessor? _commandProcessor;

        public async Task StartAsync()
        {
            Log.Information("Bot starting up...");
            
            NotificationManager.Notify("Bot", "Bot is starting up...");
            
            _commandProcessor ??= new CommandProcessor(this);
            _commandProcessor.Start();

            BanchoConnection.OnReady += OnBanchoReady;
            BanchoConnection.OnConnectionError += OnConnectionError;

            await LoadLobbiesFromDatabase();
            await BanchoConnection.StartAsync();
        }

        public async Task StopAsync()
        {
            Log.Information("Bot shutting down...");
            
            BanchoConnection.OnReady -= OnBanchoReady;
            BanchoConnection.OnConnectionError -= OnConnectionError;
            
            _commandProcessor?.Stop();
            
            foreach (var lobby in Lobbies)
            {
                await lobby.Dispose();
            }

            await BanchoConnection.StopAsync();
        }
        
        public async Task ReloadLobbies()
        {
            if (!BanchoConnection.IsConnected)
            {
                throw new InvalidOperationException("Bot is not connected to Bancho.");
            }
            
            await LoadLobbiesFromDatabase();

            foreach (var lobby in Lobbies.Where(lobby => !(lobby.Health == LobbyHealth.Ok || lobby.Health == LobbyHealth.Idle)))
            {
                await lobby.ConnectAsync();
            }
        }
        
        public async Task DeleteLobby(int configurationId)
        {
            await using var context = new BotDbContext();
            
            var lobby = Lobbies.FirstOrDefault(x => x.LobbyConfigurationId == configurationId);

            if (lobby == null)
            {
                throw new InvalidOperationException("Failed to find lobby configuration.");
            }

            await lobby.Dispose();
            
            Lobbies.Remove(lobby);
            OnLobbyRemoved?.Invoke(lobby);

            context.Remove(lobby);
            
            await context.SaveChangesAsync();
        }
        
        private async Task LoadLobbiesFromDatabase()
        {
            await using var context = new BotDbContext();
         
            Log.Verbose("Bot: Loading lobby configurations...");

            var lobbyConfigurations = await context.LobbyConfigurations.ToListAsync();
            
            foreach (var lobbyConfiguration in lobbyConfigurations.Where(lobbyConfiguration => !Lobbies.Any(x => x.LobbyConfigurationId == lobbyConfiguration.Id)))
            {
                var lobby = new Lobby(this, lobbyConfiguration.Id);
                
                Lobbies.Add(lobby);
                
                OnLobbyCreated?.Invoke(lobby);
                
                Log.Information("Bot: Loaded lobby configuration with id {LobbyConfigurationId}", lobbyConfiguration.Id);
            }

            Lobbies.Sort((x, y) => x.LobbyConfigurationId.CompareTo(y.LobbyConfigurationId));
        }

        private async void OnBanchoReady()
        {
            foreach (var lobby in Lobbies)
            {
                Log.Information("Bot: Starting lobby with id {LobbyConfigurationId}...", lobby.LobbyConfigurationId);
                
                await lobby.ConnectAsync();
                
                var attempts = 0;
                while (!(lobby.Health == LobbyHealth.Ok || lobby.Health == LobbyHealth.Idle))
                {
                    if (attempts++ > 20)
                    {
                        Log.Error("Bot: Lobby with id {LobbyIndex} did not become ready after 10 seconds.", lobby.LobbyConfigurationId);
                        lobby.Health = LobbyHealth.ChannelCreationFailed;
                        break;
                    }

                    // If something went wrong with the connection, we should stop trying.
                    if (BanchoConnection.ConnectionCancellationToken?.IsCancellationRequested == true)
                    {
                        break;
                    }
                    
                    await Task.Delay(500);
                }
            }
            
            NotificationManager.Notify("Bot", "Bot is now ready");
        }

        private void OnConnectionError()
        {
            NotificationManager.Notify("Bot", "Bot has lost connection to Bancho");
        }
        
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            await Task.WhenAny(StopAsync(), Task.Delay(5000));
        }
    }
}