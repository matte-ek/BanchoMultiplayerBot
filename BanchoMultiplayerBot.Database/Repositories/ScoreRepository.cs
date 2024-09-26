using BanchoMultiplayerBot.Database.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanchoMultiplayerBot.Database.Repositories;

public class ScoreRepository : BaseRepository<Score>
{
    public async Task<int?> GetMapPlayCountByLobbyIdAsync(int lobbyId, int mapId)
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

        var mapPosition = await BotDbContext.MapPositions
            .FromSqlRaw(query, [new NpgsqlParameter("lobbyId", lobbyId), new NpgsqlParameter("mapId", mapId)])
            .FirstOrDefaultAsync();
        
        return mapPosition?.RowNumber;
    }
    
    public async Task<IReadOnlyList<Score>> GetScoresByMapIdAsync(int mapId, int count = 10)
    {
        return await BotDbContext.Scores
            .Where(x => x.BeatmapId == mapId)
            .OrderByDescending(x => x.Time)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Score>> GetScoresByMapAndPlayerIdAsync(int playerId, int mapId)
    {
        return await BotDbContext.Scores
            .Where(x => x.PlayerId == playerId && x.BeatmapId == mapId)
            .OrderByDescending(x => x.Time)
            .ToListAsync();
    }

    public async Task<Score?> GetPlayerBestScoreAsync(int playerId)
    {
        return await BotDbContext.Scores
            .Where(x => x.PlayerId == playerId)
            .OrderByDescending(x => x.TotalScore)
            .FirstOrDefaultAsync();
    }
    
    public async Task<Score?> GetMapBestScore(int mapId)
    {
        return await BotDbContext.Scores
            .Where(x => x.BeatmapId == mapId)
            .OrderByDescending(x => x.TotalScore)
            .Include(x => x.User)
            .FirstOrDefaultAsync();
    }
}