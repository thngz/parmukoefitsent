using App.Models;

namespace App.Infrastructure.Interfaces;

public interface IScrapeWorker
{
    // public List<Product> GetProductsOnPage();
    public void Work();
}