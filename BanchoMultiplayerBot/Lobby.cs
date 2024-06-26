using BanchoMultiplayerBot.Bancho;
using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using System.Text.Json.Nodes;
using BanchoMultiplayerBot.Configuration;
using BanchoSharp.Interfaces;

namespace BanchoMultiplayerBot
{
    public abstract class Lobby(BanchoConnection banchoConnection) : ILobby
    {
        public BanchoConnection BanchoConnection { get; init; } = banchoConnection;
        
        public MultiplayerLobby? MultiplayerLobby { get; private set; } = null;

        public int LobbyConfigurationId { get; set; } = new();

        public JsonObject RuntimeData { get; } = new();

        private readonly List<IBehavior> _behaviors = [];

        private string _channelId = string.Empty;
        
        public async Task ConnectAsync(string? existingChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                throw new InvalidOperationException("BanchoClient is not initialized during lobby connection attempt.");
            }
            
            _channelId = existingChannel ?? string.Empty;

            if (existingChannel != null)
            {
                await BanchoConnection.BanchoClient?.SendAsync($"JOIN {existingChannel}")!;

                MultiplayerLobby = new MultiplayerLobby(BanchoConnection.BanchoClient, long.Parse(existingChannel[4..]), existingChannel);
                
                BuildInstance();
            }
            else
            {
                await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(LobbyConfiguration.Name.Length != 0 ? LobbyConfiguration.Name : "BMB Lobby")!;
            }
        }
        
        public void Setup()
        {
            BanchoConnection.ChannelHandler.OnLobbyCreated += OnLobbyCreated;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure += OnChannelJoinedFailure;
        }

        public void Dispose()
        {
            BanchoConnection.ChannelHandler.OnLobbyCreated -= OnLobbyCreated;
            BanchoConnection.ChannelHandler.OnChannelJoinFailure -= OnChannelJoinedFailure;
            
            ShutdownInstance();
        }

        public T? GetBehavior<T>()
        {
            return (T?)_behaviors.FirstOrDefault(x => x.GetType() == typeof(T));
        }
        
        private void BuildInstance()
        {
            if (_behaviors.Count > 0)
            {
                ShutdownInstance();
            }
        }

        private void ShutdownInstance()
        {
            foreach (var behavior in _behaviors)
            {
                behavior.Dispose();
            }

            _behaviors.Clear();
        }
        
        private void OnLobbyCreated(IMultiplayerLobby lobby)
        {
            MultiplayerLobby = lobby as MultiplayerLobby;
            
            BuildInstance();
        }

        private async void OnChannelJoinedFailure(string attemptedChannel)
        {
            if (BanchoConnection.BanchoClient == null)
            {
                return;
            }
            
            if (attemptedChannel != _channelId)
            {
                // Not the channel we were trying to join, ignore
                return;
            }
            
            await BanchoConnection.BanchoClient?.MakeTournamentLobbyAsync(LobbyConfiguration.Name.Length != 0 ? LobbyConfiguration.Name : "BMB Lobby")!;
        }
    }
}
