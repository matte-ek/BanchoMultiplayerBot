using BanchoMultiplayerBot.Osu.Models;
using BanchoSharp.Multiplayer;
using OsuSharp.Models.Scores;

namespace BanchoMultiplayerBot.Data;

public class PlayerScoreResult(MultiplayerPlayer player, Score? score)
{
    public MultiplayerPlayer Player { get; } = player;

    public Score? Score { get; } = score;
}