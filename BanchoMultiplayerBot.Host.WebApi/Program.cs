using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Setup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .WriteTo.File("log.txt",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Migrate the database 
{
    await using var context = new BotDbContext();
    context.Database.Migrate();
}

// Load configuration
var userConfig = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var configuration = new BotConfiguration
{
    BanchoClientConfiguration = new BanchoClientConfiguration()
    {
        IsBotAccount = false,
        Username = userConfig["OsuUsername"]!,
        Password = userConfig["OsuPassword"]!
    },
    OsuApiKey = userConfig["OsuApiKey"]!
};

builder.Services.AddSingleton(new Bot(configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.GetRequiredService<Bot>().StartAsync();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
