using System.Text.Json;
using BanchoMultiplayerBot.Interfaces;
using System.Text.Json.Nodes;

namespace BanchoMultiplayerBot.Behaviors
{
    public class HostQueueBehavior : IBehavior
    {
        private Lobby _lobby = null!;
        
        public List<string> Queue
        {
            get
            {
                _lobby.RuntimeData["queue"] ??= new JsonArray();
                return _lobby.RuntimeData["queue"]!.GetValue<string[]>().ToList();
            }
            
            set => _lobby.RuntimeData["queue"] = JsonSerializer.Serialize(value);
        }
        
        public void Setup(Lobby lobby)
        {
            _lobby = lobby;
            
            Queue.Insert(0, "hello world.");
        }

        public void Dispose()
        {
        }
    }
}