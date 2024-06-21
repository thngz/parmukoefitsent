using App.DAL;
using App.Models;
using OpenQA.Selenium;

namespace App.Infrastructure.Interfaces.ServiceInterfaces;

public interface ISeleniumService
{
    public IWebDriver CreateDriver();
    
    public IEnumerable<Product> GetProductsOnPage(string productCssSelector,
        Func<IWebElement, Store, Product> CreateProduct,
        IWebDriver driver, Store store);
}