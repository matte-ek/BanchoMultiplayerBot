using BanchoMultiplayerBot.Behaviour;
using BanchoMultiplayerBot.Host.Web.Pages;
using BanchoSharp.Interfaces;
using Serilog;

namespace BanchoMultiplayerBot.Host.Web.Statistics
{
    public class StatisticsTrackerService
    {
        private readonly BotService _bot;

        public StatisticsMinuteSnapshot? MinuteSnapshot { get; set; }

        private int _currentMessagesSent;
        private int _currentMessagesReceived;
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
                _bot.Client.OnPrivateMessageReceived += OnMessageReceived;
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

                    var mapManagerBehaviour = (MapManagerBehaviour?)x.Behaviours.Find(behaviour => behaviour.GetType() == typeof(MapManagerBehaviour));
                    if (mapManagerBehaviour != null)
                    {
                        lobbyMinuteSnapshots.Add(new StatisticsLobbyMinuteSnapshot()
                        {
                            Name = x.Configuration.Name,
                            MapId = mapManagerBehaviour.CurrentBeatmapId,
                            MapSetId = mapManagerBehaviour.CurrentBeatmapSetId,
                            MapName = mapManagerBehaviour.CurrentBeatmapName,
                            Players = x.MultiplayerLobby.Players.Count,
                            TotalGamesPlayed = x.GamesPlayed,
                            TotalHostViolations = x.HostViolationCount,
                            GamesPlayed = x.GamesPlayed - (MinuteSnapshot?.Lobbies.Find(lobby => lobby.Name == x.Configuration.Name)?.TotalGamesPlayed ?? 0),
                            HostViolations = x.HostViolationCount - (MinuteSnapshot?.Lobbies.Find(lobby => lobby.Name == x.Configuration.Name)?.TotalHostViolations ?? 0)
                        });
                    }
                });

                MinuteSnapshot = new StatisticsMinuteSnapshot()
                {
                    MessagesSent = _currentMessagesSent,
                    MessageThroughput = _currentMessagesSent + _currentMessagesReceived,
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
                _currentMessagesReceived = 0;

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private void OnMessageSent(IPrivateIrcMessage message)
        {
            _currentMessagesSent++;
        }

        private void OnMessageReceived(IPrivateIrcMessage message)
        {
            _currentMessagesReceived++;
        }
    }
}
