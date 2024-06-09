using BanchoMultiplayerBot.Interfaces;
using BanchoSharp.Multiplayer;
using System.Text.Json.Nodes;

namespace BanchoMultiplayerBot
{
    public class Lobby : ILobby
    {
        public List<IBehavior> Behaviors { get; set; } = [];

        public MultiplayerLobby MultiplayerLobby { get; set; }

        public Lobby(MultiplayerLobby lobby)
        {
            MultiplayerLobby = lobby;
        }

        public void Setup()
        {
            
        }

        public void Dispose()
        {
            
        }

        public T GetBehavior<T>()
        {
            
        }

        public void Load(JsonObject data)
        {
            
        }

        public JsonObject Save()
        {
            
        }
    }
}
