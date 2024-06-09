using System.Text.Json.Nodes;

namespace BanchoMultiplayerBot.Interfaces
{
    public interface IBehavior
    {
        public void Setup();
     
        public void Dispose();

        /// <summary>
        /// Load arbitrary session data from a JSON object
        /// </summary>
        public void Load(JsonObject data);

        /// <summary>
        /// Save arbitrary session data to a JSON object
        /// </summary>
        /// <returns></returns>
        public JsonObject Save();
    }
}
