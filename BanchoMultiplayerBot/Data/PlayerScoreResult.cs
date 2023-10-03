using BanchoMultiplayerBot.OsuApi;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class PlayerScoreResult
{
    public MultiplayerPlayer Player { get; }
    public ScoreModel? Score { get; } = null;

    public PlayerScoreResult(MultiplayerPlayer player, ScoreModel? score)
    {
        Player = player;
        Score = score;
    }
}