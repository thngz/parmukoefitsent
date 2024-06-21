namespace App.DAL.Repositories;

public interface IRepository<TEntity>
{
    public void UpsertEntities(IEnumerable<TEntity> entities);
    public TEntity? GetEntity(int id);
    public TEntity? GetEntity(string name);

    // Returns element names which are inserted into database, for efficient upsert later
    public HashSet<string> GetEntityNamesInDb();
}