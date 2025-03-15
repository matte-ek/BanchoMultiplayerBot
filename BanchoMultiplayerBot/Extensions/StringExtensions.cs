namespace BanchoMultiplayerBot.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Within Bancho, all spaces are replaced with underscores for usernames.
    /// Since this is a common operation, we have this extension method to make it easier.
    /// </summary>
    public static string ToIrcNameFormat(this string str)
    {
        return str.Replace(' ', '_');
    }
}