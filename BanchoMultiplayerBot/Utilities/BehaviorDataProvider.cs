using BanchoMultiplayerBot.Database;
using BanchoMultiplayerBot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BanchoMultiplayerBot.Utilities;

public sealed class BehaviorDataProvider<T> : IDisposable, IAsyncDisposable where T : class
{
    public T Data = null!;

    private readonly ILobby _lobby;
    
    public BehaviorDataProvider(ILobby lobby)
    {
        _lobby = lobby;
        
        using var dbContext = new BotDbContext();

        var data = dbContext.LobbyBehaviorData.FirstOrDefault(x => x.LobbyConfigurationId == lobby.LobbyConfigurationId);
        if (data == null)
        {
            return;
        }

        Data = JsonConvert.DeserializeObject<T>(data.Data) ?? throw new InvalidOperationException();
    }
    
    public void Dispose()
    {
        Task.Run(SaveData).Wait();
    }

    public async ValueTask DisposeAsync()
    {
        await SaveData();
    }

    private async Task SaveData()
    {
        await using var dbContext = new BotDbContext();

        var data = await dbContext.LobbyBehaviorData.FirstOrDefaultAsync(x => x.LobbyConfigurationId == _lobby.LobbyConfigurationId);
        if (data == null)
        {
            return;
        }
        
        data.Data = JsonConvert.SerializeObject(Data);
    }
}