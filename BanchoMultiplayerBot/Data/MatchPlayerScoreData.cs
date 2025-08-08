using osu.NET.Models.Scores;

namespace BanchoMultiplayerBot.Data;

public class MatchPlayerScoreData(IReadOnlyList<PlayerScoreResult> playerScores, Score[] leaderboardScores)
{
    public IReadOnlyList<PlayerScoreResult> RecentPlayerScores { get; } = playerScores;
    
    public Score[] LeaderboardScores { get; } = leaderboardScores;
}