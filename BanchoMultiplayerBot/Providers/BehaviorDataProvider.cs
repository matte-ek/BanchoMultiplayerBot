using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Database.Models;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace BanchoMultiplayerBot.Providers;

public sealed class BehaviorDataProvider<T> where T : class
{
    public readonly T Data = null!;

    private readonly ILobby _lobby;
    
    public BehaviorDataProvider(ILobby lobby)
    {
        _lobby = lobby;

        using var dbContext = new BotDbContext();

        var typeName = typeof(T).Name;

        var data = dbContext.LobbyBehaviorData.FirstOrDefault(x => x.LobbyConfigurationId == lobby.LobbyConfigurationId && x.BehaviorName == typeName);
        if (data == null)
        {
            Log.Verbose("BehaviorDataProvider: Unable to find data for {BehaviorDataType}, creating new one", typeName);

            Data = (T)Activator.CreateInstance(typeof(T))!;

            return;
        }

        Data = JsonConvert.DeserializeObject<T>(data.Data) ?? throw new InvalidOperationException();
    }

    public async Task SaveData()
    {
        await using var dbContext = new BotDbContext();

        var typeName = typeof(T).Name;

        var data = await dbContext.LobbyBehaviorData.FirstOrDefaultAsync(x => x.LobbyConfigurationId == _lobby.LobbyConfigurationId && x.BehaviorName == typeName);
        
        if (data == null)
        {
            data = new LobbyBehaviorData()
            {
                LobbyConfigurationId = _lobby.LobbyConfigurationId,
                BehaviorName = typeName,
                Data = JsonConvert.SerializeObject(Data)
            };

            dbContext.LobbyBehaviorData.Add(data);

            await dbContext.SaveChangesAsync();

            return;
        }
        
        data.Data = JsonConvert.SerializeObject(Data);

        await dbContext.SaveChangesAsync();
    }
}