using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using BanchoMultiplayerBot;
using BanchoMultiplayerBot.Bancho.Data;
using BanchoMultiplayerBot.Data;
using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Host.WebApi.Extensions;
using BanchoMultiplayerBot.Host.WebApi.Hubs;
using BanchoMultiplayerBot.Host.WebApi.Providers;
using BanchoMultiplayerBot.Host.WebApi.Services;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
builder.Services.AddSignalR();

// Setup and migrate database
await using (var context = new BotDbContext())
{
    await context.Database.MigrateAsync();
}

// Setup services
builder.Services.AddSingleton<IBotConfiguration>(new BotConfigurationProvider(builder.Configuration));
builder.Services.AddSingleton<Bot>();

builder.Services.AddScoped<BehaviorService>();
builder.Services.AddScoped<LobbyService>();

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
}

//await app.Services.GetRequiredService<Bot>().StartAsync();

app.UseCors("DefaultPolicy");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LobbyEventHub>("/hubs/lobby");

await app.RunAsync();

await app.Services.GetRequiredService<Bot>().StopAsync();