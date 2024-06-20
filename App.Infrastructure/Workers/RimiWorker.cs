using System.Collections.ObjectModel;
using System.Diagnostics;
using App.DAL;
using App.Infrastructure.Interfaces;
using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace App.Infrastructure.Workers;

public class RimiUrl
{
    private int _pageSize = 80;
    private const string BaseUrl = "https://www.rimi.ee/epood/ee/tooted/alkohol/c/SH-1?";

    public string GetUrl(int index)
    {
        return $"{BaseUrl}currentPage={index}&pageSize={_pageSize}";
    }
}

public class RimiWorker : IScrapeWorker
{
    private readonly RimiUrl _url = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RimiWorker> _logger;
    private readonly Store _store;
    private readonly IProductBehavior _productBehavior;
    private readonly ISeleniumBehavior _seleniumBehavior;


    public RimiWorker(IServiceProvider serviceProvider, ILogger<RimiWorker> logger, IProductBehavior productBehavior,
        ISeleniumBehavior seleniumBehavior)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _productBehavior = productBehavior;
        _seleniumBehavior = seleniumBehavior;
        _store = GetStore().Result;
    }

    public void Work(int maxThreadCount)
    {
        var initialDriver = _seleniumBehavior.CreateDriver();
        initialDriver.Navigate().GoToUrl(_url.GetUrl(1));
        var paginationList = initialDriver.FindElement(By.ClassName("pagination__list"));
        var paginationItems = paginationList.FindElements(By.ClassName("pagination__item"));
        var lastPage = int.Parse(paginationItems.ElementAt(paginationItems.Count - 2).Text);
        initialDriver.Quit();

        Parallel.For(1,
            lastPage,
            new ParallelOptions { MaxDegreeOfParallelism = maxThreadCount },
            StartProcessingPages);
    }

    private void StartProcessingPages(int index)
    {
        var driver = _seleniumBehavior.CreateDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        driver.Navigate().GoToUrl(_url.GetUrl(index));
        ClickCookieBanner(driver);
        _logger.LogInformation($"starting {index}th page ");
        var products = _productBehavior.GetProductsOnPage(".product-grid__item", CreateInitialProduct, driver);

        using (var scope = _serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetService<AppDbContext>();
            Trace.Assert(ctx is not null);
            
            _logger.LogInformation($"Enumerating products not in db");
            foreach (var product in products)
            {
                driver.Navigate().GoToUrl(product.ProductUrl);
                GetAdditionalData(product, driver);

                _logger.LogInformation($"{product.Name} additional data gotten");

                product.Coefficient = Math.Round(product.AlcContent * product.Amount / product.Price, 2);
                _productBehavior.InsertProduct(product, ctx);
            }

            ctx.SaveChanges();
        }

        driver.Quit();
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

    private void GetAdditionalData(Product product, IWebDriver driver)
    {
        var productDetails = new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
        try
        {
            productDetails = driver.FindElement(By.ClassName("list")).FindElements(By.ClassName("item"));
        }
        catch (NoSuchElementException)
        {
            _logger.LogError("Trying again!");
            driver.Navigate().Refresh();
            GetAdditionalData(product, driver);
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
        catch (NoSuchElementException)
        {
            _logger.LogInformation("Didnt find the cookie element");
        }
    }

    private async Task<Store> GetStore()
    {
        using var scope = _serviceProvider.CreateScope();

        var ctx = scope.ServiceProvider.GetService<AppDbContext>();

        Trace.Assert(ctx is not null);
        var store = ctx.Stores.FirstOrDefault(s => s.Name == "Rimi");

        if (store is null)
        {
            ctx.Stores.Add(new Store { Name = "Rimi" });
            await ctx.SaveChangesAsync();
            store = ctx.Stores.FirstOrDefault(s => s.Name == "Rimi");
        }

        Trace.Assert(store is not null);
        return store;
    }
}