using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Bancho.Interfaces
{
    public interface IEventsHandler
    {
        public event Action? OnMatchStarted;
        
        public event Action? OnMatchFinished;

        public event Action? OnMatchAborted;

        public event Action<MultiplayerPlayer>? OnPlayerJoined;

        public event Action<MultiplayerPlayer>? OnPlayerDisconnected;

        public event Action? OnSettingsUpdated;

        public event Action<MultiplayerPlayer>? OnHostChanged;

        public event Action<MultiplayerPlayer>? OnHostChangingMap;

        public event Action<BeatmapShell>? OnBeatmapChanged;

        public void Start();
        public void Stop();
    }
}
