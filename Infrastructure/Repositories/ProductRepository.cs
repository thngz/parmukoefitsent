using Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProductRepository(AppDbContext context) : IRepository<Product>
{
    public void UpsertEntities(IEnumerable<Product> entities)
    {
        context.UpsertRange(entities).On(p => p.ProductUrl).Run();
        context.BulkSaveChanges();
    }

    public Product? GetEntity(int id)
    {
        return context.Products.FirstOrDefault(p => p.Id == id);
    }

    public Product? GetEntity(string name)
    {
        return context.Products.FirstOrDefault(p => p.ProductUrl == name);
    }

    public HashSet<string> GetEntityNamesInDb()
    {
        var urls = context.Products.Select(x => x.ProductUrl);
        return [..context.Products.Where(x => urls.Contains(x.ProductUrl)).Select(x => x.ProductUrl)];
    }
}
