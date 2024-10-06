using System.Diagnostics;
using Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using Infrastructure.Repositories;

namespace Infrastructure.Services;

public class SeleniumService
{
    public IWebDriver CreateDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        return new FirefoxDriver(options);
    }

    public IEnumerable<Product> GetProductsOnPage(string productCssSelector,
        Func<IWebElement, Store, Product> createProduct, IWebDriver driver, Store store)
    {
        var productsOnPage = driver.FindElements(By.CssSelector(productCssSelector));
        return productsOnPage.Select(element => createProduct(element, store)).ToList();
    }

    public void UpsertProducts(IEnumerable<Product> products, AppDbContext context)
    {
        context.UpsertRange(products).On(p => p.ProductUrl).Run();
        context.BulkSaveChanges();
    }

    public Store GetStoreForProduct(string storeName, AppDbContext context)
    {
        var store = context.Stores.FirstOrDefault(s => s.Name == storeName);

        if (store is null)
        {
            context.Stores.Add(new Store { Name = storeName });
            context.SaveChanges();
            store = context.Stores.FirstOrDefault(s => s.Name == storeName);
        }

        Trace.Assert(store is not null);
        return store;
    }
}
