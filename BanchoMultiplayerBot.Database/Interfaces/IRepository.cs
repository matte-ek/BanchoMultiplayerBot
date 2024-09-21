namespace BanchoMultiplayerBot.Database.Interfaces;

/// <summary>
/// Generic repository interface for default CRUD operations
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(int id);
    
    Task<IReadOnlyList<T>> GetAllAsync();
    
    Task AddAsync(T entity);
    
    Task AddRangeAsync(IEnumerable<T> entities);
    
    void Delete(T entity);
    
    void DeleteRange(IEnumerable<T> entities);
    
    Task<long> CountAsync();
    
    Task SaveAsync();
}