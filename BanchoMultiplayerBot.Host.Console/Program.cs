
using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Config;

var bot = new Bot("config.json");

bot.OnBotReady += async () =>
{
    var fakeConfig = new LobbyConfiguration
    {
        Name = "Test lobby",
        Size = 16,
        Password = ""
    };

    await bot.AddLobbyAsync("#mp_105079765", fakeConfig);
};

await bot.RunAsync();