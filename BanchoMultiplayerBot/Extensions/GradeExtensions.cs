using OsuSharp.Enums;

namespace BanchoMultiplayerBot.Extensions;

public static class GradeExtensions
{
    public static int GetRankId(this Grade grade)
    {
        return grade switch
        {
            Grade.XH => 7,
            Grade.X => 7,
            Grade.SH => 7,
            Grade.S => 6,
            Grade.A => 5,
            Grade.B => 4,
            Grade.C => 3,
            Grade.D => 2,
            Grade.F => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, null)
        };
    }
    
    public static string AsHumanString(this Grade grade)
    {
        return grade switch
        {
            Grade.XH => "SS",
            Grade.X => "SS",
            Grade.SH => "S",
            Grade.S => "S",
            Grade.A => "A",
            Grade.B => "B",
            Grade.C => "C",
            Grade.D => "D",
            Grade.F => "F",
            _ => throw new ArgumentOutOfRangeException(nameof(grade), grade, null)
        };
    }
}