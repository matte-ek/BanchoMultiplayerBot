namespace BanchoMultiplayerBot.Osu;

public class BeatmapProcessor
{
    // Attempt to find the first hit object in seconds, relies on the map being downloaded beforehand
    public static async Task<int?> GetBeatmapStartTime(int beatmapId)
    {
        const string cacheDir = $"cache";
        
        var beatmapFilePath = $"{cacheDir}/{beatmapId}.osu";

        try
        {
            if (!File.Exists(beatmapFilePath))
            {
                return null;
            }

            var inHitObjectsSection = false;
            
            foreach (var line in await File.ReadAllLinesAsync(beatmapFilePath))
            {
                if (!line.Any())
                {
                    continue;
                }
                
                if (line.StartsWith("[HitObjects]"))
                {
                    inHitObjectsSection = true;
                    continue;
                }

                if (!inHitObjectsSection)
                {
                    continue;
                }
                
                return int.Parse(line.Split(',')[2]) / 1000;
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return null;
    }
}