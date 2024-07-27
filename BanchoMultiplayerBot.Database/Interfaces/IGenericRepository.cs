namespace BanchoMultiplayerBot.Database.Interfaces;

public interface IGenericRepository<TEntity> where TEntity : class
{
    TEntity GetById(int id);
    IEnumerable<TEntity> GetAll();
    
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
}