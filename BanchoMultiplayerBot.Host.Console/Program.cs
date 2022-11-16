
using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Config;

var bot = new Bot("config.json");

bot.OnBotReady += async () =>
{
    var fakeConfig = new LobbyConfiguration
    {
        Name = "Test lobby",
        Size = 16,
        Password = "",
        Behaviours = new [] { "AutoHostRotate" }, 
        LimitStarRating = true,
        MaximumStarRating = 6f,
        MinimumStarRating = 4.5f,
        StarRatingErrorMargin = 0.1f,
        LimitMapLength = true,
        MaximumMapLength = 300
    };

    await bot.AddLobbyAsync("#mp_105094051", fakeConfig);
};

await bot.RunAsync();