using App.DAL;
using App.Infrastructure.Interfaces;
using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace App.Infrastructure.Behaviors;

public class ProductBehavior(ILogger<ProductBehavior> logger) : IProductBehavior
{
    public IEnumerable<Product> GetProductsOnPage(string productCssSelector, Func<IWebElement, Product> createProduct, IWebDriver driver)
    {
        var productsOnPage = driver.FindElements(By.CssSelector(productCssSelector));
        return productsOnPage.Select(createProduct).ToList();
    }

    public void UpsertProducts(IEnumerable<Product> products, AppDbContext context)
    {
        logger.LogInformation("Upserting products");
        context.Products.UpsertRange(products).On(p => p.ProductUrl).Run();
    }

    public void UpdateProduct(Product product, AppDbContext context)
    {
        context.Products.Update(product);
    }

    public void InsertProduct(Product product, AppDbContext context)
    {
        context.Products.Add(product);
    }
}