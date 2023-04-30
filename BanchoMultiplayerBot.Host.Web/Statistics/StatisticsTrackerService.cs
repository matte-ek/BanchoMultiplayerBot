using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsTrackerService
    {
        private readonly BotService _bot;

        public StatisticsMinuteSnapshot? MinuteSnapshot { get; set; }

        private int _currentMessagesSent;
        private int _currentGamesPlayed;
        private int _currentPpCalcSuccess;
        private int _currentPpCalcError;

        public StatisticsTrackerService(BotService bot)
        {
            _bot = bot;
        }

        public void Start()
        {
            Task.Run(StatisticsTrackerTask);
        }

        private async Task StatisticsTrackerTask()
        {
            _bot.OnBotReady += () =>
            {
                _bot.Client.OnPrivateMessageSent += OnMessageSent;
            };

            while (true)
            {
                var lobbyMinuteSnapshots = new List<StatisticsLobbyMinuteSnapshot>();

                int gamesPlayed = 0;
                int playerCount = 0;
                int ppCalcSuccess = 0;
                int ppCalcError = 0;

                _bot.Lobbies.ForEach(x =>
                {
                    playerCount += x.MultiplayerLobby.Players.Count;
                    gamesPlayed += x.GamesPlayed;
                    ppCalcSuccess += x.PerformanceCalculationSuccessCount;
                    ppCalcError += x.PerformanceCalculationErrorCount;

                    lobbyMinuteSnapshots.Add(new StatisticsLobbyMinuteSnapshot()
                    {
                        Name = x.Configuration.Name,
                        Players = x.MultiplayerLobby.Players.Count,
                        GamesPlayed = x.GamesPlayed - (MinuteSnapshot?.Lobbies.Find(lobby => lobby.Name == x.Configuration.Name)?.GamesPlayed ?? 0),
                        HostViolations = x.HostViolationCount - (MinuteSnapshot?.Lobbies.Find(lobby => lobby.Name == x.Configuration.Name)?.HostViolations ?? 0)
                    });
                });

                MinuteSnapshot = new StatisticsMinuteSnapshot()
                {
                    MessagesSent = _currentMessagesSent,
                    GamesPlayed = gamesPlayed - _currentGamesPlayed,
                    TotalPlayers = playerCount,
                    PerformancePointCalcSuccessCount = ppCalcSuccess - _currentPpCalcSuccess,
                    PerformancePointCalcErrorCount = ppCalcError - _currentPpCalcError,
                    Lobbies = lobbyMinuteSnapshots
                };

                _currentPpCalcSuccess = ppCalcSuccess;
                _currentPpCalcError = ppCalcError;
                _currentGamesPlayed = gamesPlayed;
                _currentMessagesSent = 0;

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private void OnMessageSent(IPrivateIrcMessage message)
        {
            _currentMessagesSent++;
        }
    }
}
