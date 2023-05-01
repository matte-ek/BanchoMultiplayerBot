using MudBlazor;

namespace BanchoMultiplayerBot.Status.Data
{
    public class LobbyData
    {

        public string Name { get; set; } = string.Empty;

        public int Players { get; set; }

        public double AveragePlayers {get; set; }

        public int GamesLastHour { get; set; }

        public string MapName { get; set; }
        public int MapId { get; set; }
        public int MapSetId { get; set; }

        public List<ChartSeries> Chart { get; set; } = null!;
    }
}
