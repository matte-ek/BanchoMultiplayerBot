namespace BanchoMultiplayerBot.Host.Web.Extra
{

    /// <summary>
    /// Output caching is not yet a thing in ASP Core for .NET 6.0, so this will do. 
    /// </summary>
    public class BannerCacheService
    {
        public string OutputCache { get; set; } = string.Empty;
        public DateTime CacheUpdateTime { get; set; }

        public BannerCacheService() { }
    }
}
