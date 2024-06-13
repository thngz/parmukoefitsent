using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using App.DAL;
using App.Infrastructure.Interfaces;
using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace App.Infrastructure.Workers;

public class RimiUrl
{
    private int _currentPage = 1;
    private int _pageSize = 20;
    private const string BaseUrl = "https://www.rimi.ee/epood/ee/tooted/alkohol/c/SH-1?";

    public string GetUrl(int index)
    {
        return $"{BaseUrl}currentPage={index}&pageSize={_pageSize}";
    }
}

public class RimiWorker : IScrapeWorker
{
    private readonly RimiUrl _url = new();
    private readonly AppDbContext _context;
    private readonly ILogger<RimiWorker> _logger;
    private readonly Store _store;

    public RimiWorker(AppDbContext context, ILogger<RimiWorker> logger)
    {
        _context = context;
        _logger = logger;
        _store = GetStore().Result;
    }

    public void Work()
    {
        var initialDriver = CreateDriver();
        initialDriver.Navigate().GoToUrl(_url.GetUrl(1));
        var paginationList = initialDriver.FindElement(By.ClassName("pagination__list"));
        var paginationItems = paginationList.FindElements(By.ClassName("pagination__item"));
        var lastPage = int.Parse(paginationItems.ElementAt(paginationItems.Count - 2).Text);

        var _lock = new object();
        initialDriver.Quit();
        
        // let each thread work on a range
        Parallel.For(1, lastPage, new ParallelOptions { MaxDegreeOfParallelism = 4 }, index =>
        {
            var driver = CreateDriver();
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            driver.Navigate().GoToUrl(_url.GetUrl(index));
            ClickCookieBanner(driver);
            _logger.LogInformation($"starting {index}th page ");
            var products = GetProductsOnPage(driver);
            foreach (var product in products)
            {
                _logger.LogInformation($"Visiting {product.ProductUrl}");
                driver.Navigate().GoToUrl(product.ProductUrl);
            
                CalculateCoefficient(product, driver);
            
                _logger.LogInformation($"{product.Name} object made");
            }
            UpsertProducts(products); 
            _context.SaveChanges();
            driver.Quit();
        });
    }


    private List<Product> GetProductsOnPage(IWebDriver driver)
    {
        var productsOnPage = driver.FindElements(By.CssSelector(".product-grid__item"));
        return productsOnPage.Select(CreateInitialProduct).ToList();
    }

    private Product CreateInitialProduct(IWebElement productData)
    {
        var url = productData.FindElement(By.ClassName("card__url")).GetAttribute("href");

        var productName = productData.FindElement(By.ClassName("card__name")).Text;
        var productPriceContainer = productData.FindElement(By.ClassName("price-tag"));
        var priceEur = productPriceContainer.FindElement(By.TagName("span")).Text;
        var priceCents = productPriceContainer.FindElement(By.TagName("sup")).Text;

        var product = new Product
        {
            Name = productName,
            Store = _store,
            StoreId = _store.Id,
            Price = decimal.Parse($"{priceEur}.{priceCents}"),
            ProductUrl = url
        };

        return product;
    }

    private void CalculateCoefficient(Product product, IWebDriver driver)
    {
        var productDetails = new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
        try
        {
            productDetails = driver.FindElement(By.ClassName("list")).FindElements(By.ClassName("item"));
        }
        catch (NoSuchElementException e)
        {
            _logger.LogError("Trying again!");
            driver.Navigate().Refresh();
            CalculateCoefficient(product, driver);
        }

        foreach (var productDetail in productDetails)
        {
            var productDetailSpan = productDetail.FindElement(By.TagName("span")).Text;

            if (productDetailSpan == "Kogus")
            {
                var amount = productDetail.FindElement(By.ClassName("text")).Text.Split()[0];
                product.Amount = decimal.Parse(amount);
            }
            else if (productDetailSpan == "Alkohol")
            {
                var vol = productDetail.FindElement(By.ClassName("text")).Text.Split()[0].TrimEnd('%');
                product.AlcContent = decimal.Parse(vol);
            }
        }

        product.Coefficient = Math.Round(product.AlcContent * product.Amount / product.Price, 2);
    }


    private void UpsertProducts(IEnumerable<Product> products)
    {
        _logger.LogInformation("Upserting products");
        _context.Products.UpsertRange(products).On(p => p.ProductUrl).Run();
    }

    private void ClickCookieBanner(IWebDriver driver)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        // Agree with cookies
        try
        {
            var cookieButton = driver.FindElement(By.Id("CybotCookiebotDialogBodyLevelButtonLevelOptinDeclineAll"));
            wait.Until(d => cookieButton.Displayed);
            cookieButton.Click();
        }
        catch (NoSuchElementException e)
        {
            _logger.LogInformation("Didnt find the cookie element");
        }
    }

    private async Task<Store> GetStore()
    {
        var store = _context.Stores.FirstOrDefault(s => s.Name == "Rimi");

        if (store is null)
        {
            _context.Stores.Add(new Store { Name = "Rimi" });
            await _context.SaveChangesAsync();
            store = _context.Stores.FirstOrDefault(s => s.Name == "Rimi");
        }

        return store;
    }

    private IWebDriver CreateDriver()
    {
        var options = new FirefoxOptions();
        // options.AddArgument("--headless");
        return new FirefoxDriver(options);
    }
}