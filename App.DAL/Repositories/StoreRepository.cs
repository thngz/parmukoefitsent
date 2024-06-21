using App.Models;

namespace App.DAL.Repositories;

public class StoreRepository(AppDbContext context) : IRepository<Store>
{
    public void UpsertEntities(IEnumerable<Store> entities)
    {
        throw new NotImplementedException();
    }

    public Store? GetEntity(int id)
    {
        return context.Stores.FirstOrDefault(p => p.Id == id);
    }

    public Store? GetEntity(string name)
    {
        return context.Stores.FirstOrDefault(p => p.Name == name);
    }

    public HashSet<string> GetEntityNamesInDb()
    {
        throw new NotImplementedException();
    }
}