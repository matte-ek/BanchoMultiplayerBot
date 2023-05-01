using System.Net.Http;
using BanchoMultiplayerBot.Host.Web.Statistics;
using Newtonsoft.Json;

namespace BanchoMultiplayerBot.Status.Data
{
    public class StatisticsManagerService
    {
        private readonly HttpClient _httpClient = new();

        public List<StatisticsMinuteSnapshot?> StatisticsMinuteSnapshots { get; private set; } = new();

        public void Start()
        {
            Task.Run(StatisticsRetrieverTask);
        }

        public async Task StatisticsRetrieverTask()
        {
            while (true)
            {
                var data = await _httpClient.GetAsync("http://localhost:5000/api/statistics");

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

                StatisticsMinuteSnapshots.Insert(0, minuteSnapshot);

                if (StatisticsMinuteSnapshots.Count > (60 * 24) + 1)
                {
                    StatisticsMinuteSnapshots.RemoveAt(StatisticsMinuteSnapshots.Count - 1);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}
