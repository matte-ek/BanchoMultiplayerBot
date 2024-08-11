using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace BanchoMultiplayerBot.Providers;


public sealed class BehaviorConfigProvider<T> where T : class
{
    public readonly T Data = null!;

    private readonly ILobby _lobby;
    
    public BehaviorConfigProvider(ILobby lobby)
    {
        _lobby = lobby;

        using var dbContext = new BotDbContext();

        var typeName = typeof(T).Name;

        var data = dbContext.LobbyBehaviorConfig.FirstOrDefault(x => x.LobbyConfigurationId == lobby.LobbyConfigurationId && x.BehaviorName == typeName);
        if (data == null)
        {
            Log.Verbose("LobbyBehaviorConfig: Unable to find config for {BehaviorDataType}, creating new one", typeName);

            Data = (T)Activator.CreateInstance(typeof(T))!;
            
            dbContext.LobbyBehaviorConfig.Add(new LobbyBehaviorConfig()
            {
                LobbyConfigurationId = lobby.LobbyConfigurationId,
                BehaviorName = typeName,
                Data = JsonConvert.SerializeObject(Data)
            });

            dbContext.SaveChanges();
            
            return;
        }

        Data = JsonConvert.DeserializeObject<T>(data.Data) ?? throw new InvalidOperationException();
    }
}