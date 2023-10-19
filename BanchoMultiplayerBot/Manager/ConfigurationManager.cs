using System.Text.Json;
using BanchoMultiplayerBot.Config;
using BanchoMultiplayerBot.OsuApi;
using BanchoSharp;

namespace BanchoMultiplayerBot.Manager;

public class ConfigurationManager
{
    public BotConfiguration Configuration { get; private set; } = null!;

    private const string ConfigurationFile = "config.json";
    private readonly Bot _bot;

    public ConfigurationManager(Bot bot)
    {
        this._bot = bot;
    }

    public void LoadConfiguration()
    {
        if (!File.Exists(ConfigurationFile))
        {
            throw new Exception($"Failed to find configuration file");
        }

        var reader = File.OpenRead(ConfigurationFile);
        var config = JsonSerializer.Deserialize<BotConfiguration>(reader);

        reader.Close();
        reader.Dispose();

        Configuration = config ?? throw new Exception("Failed to read configuration file.");
        
        _bot.Client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Configuration.Username, Configuration.Password), LogLevel.Trace, false));
        _bot.OsuApi = new OsuApiWrapper(_bot, Configuration.ApiKey);
    }

    public void SaveConfiguration()
    {
        Configuration.LobbyConfigurations = new LobbyConfiguration[_bot.Lobbies.Count];

        for (int i = 0; i < _bot.Lobbies.Count; i++)
        {
            Configuration.LobbyConfigurations[i] = _bot.Lobbies[i].Configuration;
        }

        _bot.AnnouncementManager.Save();

        File.WriteAllText(ConfigurationFile, JsonSerializer.Serialize(Configuration));
    }

}