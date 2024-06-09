using BanchoMultiplayerBot.Bancho.Interfaces;

namespace BanchoMultiplayerBot.Bancho
{
    public class EventsHandler(BanchoConnection banchoConnection) : IEventsHandler
    {
        private BanchoConnection _banchoConnection = banchoConnection;

        public void Start()
        {
            if (_banchoConnection.BanchoClient == null)
            {
                return;
            }
        }

        public void Stop()
        {
        }
    }
}
