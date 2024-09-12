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

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .WriteTo.File("log.txt",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .CreateLogger();

AppDomain.CurrentDomain.UnhandledException += (_, args) =>
{
    var e = (Exception)args.ExceptionObject;

    Log.Fatal($"Unhandled exception: {e}");
};

var builder = WebApplication.CreateBuilder(args);

Log.Information("Starting BanchoMultiplayerBot with environment {Environment}", builder.Environment.EnvironmentName);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

BotDbContext.ConnectionString = builder.Configuration.GetConnectionString("Bot") ?? throw new InvalidOperationException("Database connection string not found.");

// Setup and migrate database
await using (var context = new BotDbContext())
{
//    await context.Database.MigrateAsync();
}

// Setup services
builder.Services.AddSingleton<IBotConfiguration>(new BotConfigurationProvider(builder.Configuration));
builder.Services.AddSingleton<Bot>();
builder.Services.AddSingleton<LobbyTrackerService>();

builder.Services.AddScoped<BehaviorService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddScoped<HealthService>();
builder.Services.AddScoped<MessageService>();

// Setup authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddBotCookieAuthentication(builder.Configuration)
.AddBotOsuAuth(builder.Configuration);

// Setup CORS policy
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}
else
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    
    app.UseHttpsRedirection();
}

app.Services.GetRequiredService<LobbyTrackerService>().Start();

await app.Services.GetRequiredService<Bot>().StartAsync();

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LobbyEventHub>("/hubs/lobby");

app.UseMetricServer("/api/statistics/metrics");

await app.RunAsync();

await app.Services.GetRequiredService<Bot>().StopAsync();

app.Services.GetRequiredService<LobbyTrackerService>().Stop();