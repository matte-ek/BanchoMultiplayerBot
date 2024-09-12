using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoMultiplayerBot.Database.Repositories;

public class ScoreRepository : IDisposable
{
    private readonly BotDbContext _botDbContext;
    private bool _disposed;

    public ScoreRepository()
    {
        _botDbContext = new BotDbContext();
    }

    public async Task Add(Score score)
    {
        score.Time = DateTime.UtcNow;
        
        await _botDbContext.Scores.AddAsync(score);
    }
    
    public async Task Save()
    {
        await _botDbContext.SaveChangesAsync();
    }

    public async Task<long> GetScoreCount()
    {
        return await _botDbContext.Scores.LongCountAsync();
    }

    public async Task<int?> GetMapPlayCountByLobbyId(int lobbyId, int mapId)
    {
        const string query = """
                             SELECT "BeatmapId", RowNumber
                             FROM (
                             SELECT "BeatmapId",
                                    ROW_NUMBER() OVER (ORDER BY COUNT(DISTINCT "GameId") DESC) AS RowNumber
                             FROM "Scores"
                             WHERE "LobbyId" = @lobbyId
                             GROUP BY "BeatmapId"
                             ) AS Ranked
                             WHERE "BeatmapId" = @mapId
                             """;

        var mapPosition = await _botDbContext.MapPositions
            .FromSqlRaw(query, [new NpgsqlParameter("lobbyId", lobbyId), new NpgsqlParameter("mapId", mapId)])
            .FirstOrDefaultAsync();
        
        return mapPosition?.RowNumber;
    }
    
    public async Task<IReadOnlyList<Score>> GetScoresByMapId(int mapId, int count = 10)
    {
        return await _botDbContext.Scores
            .Where(x => x.BeatmapId == mapId)
            .OrderByDescending(x => x.Time)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Score>> GetScoresByMapAndPlayerId(int playerId, int mapId)
    {
        return await _botDbContext.Scores
            .Where(x => x.PlayerId == playerId && x.BeatmapId == mapId)
            .OrderByDescending(x => x.Time)
            .ToListAsync();
    }

    public async Task<Score?> GetPlayerBestScore(int playerId)
    {
        return await _botDbContext.Scores
            .Where(x => x.PlayerId == playerId)
            .OrderByDescending(x => x.TotalScore)
            .FirstOrDefaultAsync();
    } 

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _botDbContext.Dispose();
            }
        }

        _disposed = true;
    }
}