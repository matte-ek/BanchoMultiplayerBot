using System.Text.Json.Nodes;

namespace BanchoMultiplayerBot.Interfaces
{
    public interface IBehavior
    {
        public void Setup(Lobby lobby);
     
        public void Dispose();
    }
}
