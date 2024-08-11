using BanchoMultiplayerBot.Osu.Models;
using BanchoSharp.Multiplayer;

namespace BanchoMultiplayerBot.Data;

public class PlayerScoreResult(MultiplayerPlayer player, ScoreModel? score)
{
    public MultiplayerPlayer Player { get; } = player;

    public ScoreModel? Score { get; } = score;
}