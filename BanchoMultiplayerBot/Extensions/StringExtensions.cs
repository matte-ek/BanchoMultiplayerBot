namespace BanchoMultiplayerBot.Extensions;

public static class StringExtensions
{

    public static string ToIrcNameFormat(this string str)
    {
        return str.Replace(' ', '_');
    }
    
}