using BanchoMultiplayerBot.Bancho.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Bancho
{
    public class EventsHandler(BanchoConnection banchoConnection) : IEventsHandler
    {
        public event Action? OnMatchStarted;
        public event Action? OnMatchFinished;
        public event Action? OnMatchAborted;

        public event Action<MultiplayerPlayer>? OnPlayerJoined;
        public event Action<MultiplayerPlayer>? OnPlayerDisconnected;
                
        public event Action<MultiplayerPlayer>? OnHostChanged;
        public event Action<MultiplayerPlayer>? OnHostChangingMap;

        public event Action? OnSettingsUpdated;

        public event Action<BeatmapShell>? OnBeatmapChanged;

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
