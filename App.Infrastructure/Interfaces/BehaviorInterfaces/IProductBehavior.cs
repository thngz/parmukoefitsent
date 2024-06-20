using App.DAL;
using App.Models;
using OpenQA.Selenium;

namespace App.Infrastructure.Interfaces;

public interface IProductBehavior
{
    public IEnumerable<Product> GetProductsOnPage(string productCssSelector, Func<IWebElement, Product> CreateProduct, IWebDriver driver);
    public void UpsertProducts(IEnumerable<Product> products, AppDbContext context);
    
    public void UpdateProduct(Product product, AppDbContext context);
    
    public void InsertProduct(Product product, AppDbContext context);
}