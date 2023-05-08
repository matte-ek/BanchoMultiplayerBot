
using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Config;

var bot = new Bot("config.json");

bot.OnBotReady += async () =>
{
    var fakeConfig = new LobbyConfiguration
    {
        Name = "test auto rotate lobby",
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

    await bot.CreateLobbyAsync(fakeConfig);
};

await bot.RunAsync();