
using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Config;

var bot = new Bot("config.json");

bot.OnBotReady += async () =>
{
    var fakeConfig = new LobbyConfiguration
    {
        Name = "test lobby",
        Size = 16,
        Password = "",
        Behaviours = new [] { "AutoHostRotate" }, 
        LimitStarRating = true,
        MaximumStarRating = 6.0f,
        MinimumStarRating = 4.5f,
        StarRatingErrorMargin = 0.1f,
        LimitMapLength = true,
        MaximumMapLength = 330
    };

    await bot.AddLobbyAsync("#mp_105119080", fakeConfig);
};

await bot.RunAsync();