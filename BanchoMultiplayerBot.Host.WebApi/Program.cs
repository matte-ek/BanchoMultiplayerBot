using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Host.WebApi.Extensions;
using BanchoMultiplayerBot.Host.WebApi.Hubs;
using BanchoMultiplayerBot.Host.WebApi.Providers;
using BanchoMultiplayerBot.Host.WebApi.Services;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

// Setup 30 day rolling file logging
// We also ignore verbose logs in the file, only writing information and above
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .WriteTo.File("log.txt",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .WriteTo.OpenTelemetry()
    .CreateLogger();

// This is generally done as a fail-safe to log any unhandled exceptions to the log file
// Although I would rather hope that this is never needed
AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    var e = (Exception)args.ExceptionObject;
    Log.Fatal($"Unhandled exception: {e}");
};

var builder = WebApplication.CreateBuilder(args);

// As per the ConnectionString comment, but this is needed since the bot itself doesn't use DI
BotDbContext.ConnectionString = builder.Configuration.GetConnectionString("Bot") ?? throw new InvalidOperationException("Database connection string not found.");

Log.Information("Starting BanchoMultiplayerBot with environment {Environment}", builder.Environment.EnvironmentName);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Setup services
builder.Services.AddSingleton<IBotConfiguration>(new BotConfigurationProvider(builder.Configuration));
builder.Services.AddSingleton<Bot>();
builder.Services.AddSingleton<LobbyTrackerService>();
builder.Services.AddSingleton<BannerCacheService>();
builder.Services.AddSingleton<BotHealthService>();

builder.Services.AddScoped<BehaviorService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<HealthService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<BannerService>();

// Setup osu! oauth authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddBotCookieAuthentication(builder.Configuration)
.AddBotOsuAuth(builder.Configuration);

// create a default CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration["Bot:FrontendUrl"]!);
        policy.AllowCredentials();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

// Migrate database before starting
// TODO: Look into breaking change of EF 9
/*
await using (var context = new BotDbContext())
{
    await context.Database.MigrateAsync();
}
*/

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}
else
{
    // Since we use a reverse proxy in production
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    
    app.UseHttpsRedirection();
}

// Start services
app.Services.GetRequiredService<LobbyTrackerService>().Start();
await app.Services.GetRequiredService<Bot>().StartAsync();
app.Services.GetRequiredService<BotHealthService>().Start();

// Apply CORS policy
app.UseCors("DefaultPolicy");

// Apply authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Apply routing
app.MapControllers();
app.MapHub<LobbyEventHub>("/hubs/lobby");

// Applies a Prometheus metrics endpoint
app.UseMetricServer("/api/statistics/metrics");

await app.RunAsync();