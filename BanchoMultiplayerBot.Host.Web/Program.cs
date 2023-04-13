using BanchoMultiplayerBot.Host.Web;
using BanchoMultiplayerBot.Host.Web.Auth;
using BanchoMultiplayerBot.Host.Web.Log;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using MudBlazor.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("log.txt",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
        rollingInterval: RollingInterval.Day,
        rollOnFileSizeLimit: true)
    .WriteTo.DashboardLogSink()
    .CreateLogger();

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    var e = (Exception)args.ExceptionObject;

    Log.Error($"Unhandeled exception: {e}");
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<BotService>();
builder.Services.AddMudServices();
builder.Services.AddScoped<AuthenticationStateProvider, TemporaryAuthStateProvider>();

var app = builder.Build();

app.Services.GetService<BotService>()?.Start();

app.UsePathBase("/osu-bot");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.MapBlazorHub(configureOptions: options =>
{
    options.Transports = HttpTransportType.WebSockets;
});

app.MapFallbackToPage("/_Host");

app.Run();
