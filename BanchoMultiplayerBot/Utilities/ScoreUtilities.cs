using BanchoMultiplayerBot.Database.Models;

namespace BanchoMultiplayerBot.Utilities;

public class ScoreUtilities
{
    public static float CalculateAccuracy(Score score)
    {
        return (score.Count300 * 300 + score.Count100 * 100 + score.Count50 * 50) / 
            (float)((score.Count300 + score.Count100 + score.Count50 + score.CountMiss) * 300) * 100;
    }
}