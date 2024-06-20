using System.Linq.Expressions;
using App.DAL;
using App.Models;
using OpenQA.Selenium;

namespace App.Infrastructure.Interfaces.ServiceInterfaces;

public interface IProductService
{
    public IEnumerable<Product> GetProductsOnPage(string productCssSelector,
        Func<IWebElement, Store, Product> CreateProduct,
        IWebDriver driver, Store store);

    public void UpsertProducts(IEnumerable<Product> products, AppDbContext context);

    public Store GetStoreForProduct(string storeName, AppDbContext context);
}