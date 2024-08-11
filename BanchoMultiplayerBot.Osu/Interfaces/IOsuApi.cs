using BanchoMultiplayerBot.Osu.Models;

namespace BanchoMultiplayerBot.Osu.Interfaces;

public interface IOsuApi
{
    public Task<BeatmapModel?> GetBeatmapInformation(int beatmapId, int mods = 0);

    public Task<ScoreModel?> GetRecentScore(string playerName);
    
    public Task<IReadOnlyList<ScoreModel?>> GetRecentScores(IEnumerable<string> players);
    
    public Task<IReadOnlyList<LeaderboardScoreModel>?> GetMapLeaderboardScores(int beatmapId);
}