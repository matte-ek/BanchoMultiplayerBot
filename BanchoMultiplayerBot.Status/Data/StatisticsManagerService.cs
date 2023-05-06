using System.Net.Http;
using BanchoMultiplayerBot.Database.Status.Models;
using BanchoMultiplayerBot.Database.Status.Repositories;
using BanchoMultiplayerBot.Host.Web.Statistics;
using Newtonsoft.Json;

namespace BanchoMultiplayerBot.Status.Data
{
    public class StatisticsManagerService
    {
        private readonly HttpClient _httpClient = new();

        public void Start()
        {
            Task.Run(StatisticsRetrieverTask);
        }

        public async Task StatisticsRetrieverTask()
        {
            while (true)
            {
                var data = await _httpClient.GetAsync("https://public.mattee.lol/api/statistics");

                StatisticsMinuteSnapshot? minuteSnapshot = null;

                if (data.IsSuccessStatusCode)
                {
                    try
                    {
                        minuteSnapshot = JsonConvert.DeserializeObject<StatisticsMinuteSnapshot>(await data.Content.ReadAsStringAsync());
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                await ProcessBotResponse(minuteSnapshot);
                await RemoveOldSnapshots();

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task ProcessBotResponse(StatisticsMinuteSnapshot? snapshot)
        {
            using var snapshotRepository = new BotSnapshotRepository();
            int lobbyIndex = 0;

            var data = new BotSnapshot()
            {
                Id = Guid.NewGuid(),
                Time = DateTime.Now,
                GamesPlayed = snapshot?.GamesPlayed ?? 0,
                MessagesSent = snapshot?.MessagesSent ?? 0,
                PerformancePointCalculations = snapshot?.PerformancePointCalcSuccessCount ?? 0,
                PerformancePointCalculationErrors = snapshot?.PerformancePointCalcErrorCount ?? 0
            };

            snapshot?.Lobbies.ForEach(lobby =>
            {
                data.LobbySnapshots.Add(new LobbySnapshot()
                {
                    Id = Guid.NewGuid(),
                    Time = DateTime.Now,
                    BotLobbyIndex = lobbyIndex++,
                    Name = lobby.Name,
                    GamesPlayed = lobby.GamesPlayed,
                    HostViolations = lobby.HostViolations,
                    Players = lobby.Players
                });
            });

            await snapshotRepository.AddSnapshot(data);
        }

        private static async Task RemoveOldSnapshots()
        {
            using var snapshotRepository = new BotSnapshotRepository();

            await snapshotRepository.RemoveSnapshotsPastTime(DateTime.Now - TimeSpan.FromDays(1));
        }
    }
}
