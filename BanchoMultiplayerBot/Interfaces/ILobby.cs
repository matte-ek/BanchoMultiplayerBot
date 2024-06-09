using BanchoSharp.Multiplayer;
using System.Text.Json.Nodes;

namespace BanchoMultiplayerBot.Interfaces
{
    public interface ILobby
    {
        /// <summary>
        /// The BanchoSharp multiplayer lobby instance
        /// </summary>
        public MultiplayerLobby MultiplayerLobby { get; } 

        public List<IBehavior> Behaviors { get; }

        public T GetBehavior<T>();

        public void Setup();
        
        public void Dispose();

        public void Load(JsonObject data);

        public JsonObject Save();
    }
}
