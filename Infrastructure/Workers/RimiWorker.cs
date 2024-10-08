using System.Collections.ObjectModel;
using System.Diagnostics;
using Infrastructure.Services;
using Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Infrastructure.Repositories;

namespace Infrastructure.Workers;

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
    private readonly SeleniumService _seleniumService;


    public RimiWorker(IServiceProvider serviceProvider, ILogger<RimiWorker> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _seleniumService = new SeleniumService();
    }

    public async Task Work(int maxThreadCount)
    {
        var initialDriver = _seleniumService.CreateDriver();
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
        var driver = _seleniumService.CreateDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        driver.Navigate().GoToUrl(_url.GetUrl(index));
        ClickCookieBanner(driver);
        _logger.LogInformation($"starting {index}th page ");

        using (var scope = _serviceProvider.CreateScope())
        {
            // We need to do this to avoid conflicts when multithreading
            var productRepo = scope.ServiceProvider.GetService<IRepository<Product>>();
            var storeRepo = scope.ServiceProvider.GetService<IRepository<Store>>();
           
            Trace.Assert(storeRepo is not null);
            
            var store = storeRepo.GetEntity("Rimi");
            
            Trace.Assert(store is not null);
            
            var productsOnPage =
                _seleniumService.GetProductsOnPage(".product-grid__item", CreateProduct, driver, store);
           
            Trace.Assert(productRepo is not null);
            
            var existingUrls = productRepo.GetEntityNamesInDb();
            var dbProducts = new List<Product>();
            foreach (var product in productsOnPage)
            {
                if (!existingUrls.Contains(product.ProductUrl))
                {
                    _logger.LogInformation($"Navigating to {product.ProductUrl}");
                    driver.Navigate().GoToUrl(product.ProductUrl);
                    SetProductDetails(product, driver);
                }

                dbProducts.Add(product);
            }

            productRepo.UpsertEntities(dbProducts);
        }

        driver.Quit();
    }


    private Product CreateProduct(IWebElement productData, Store store)
    {
        var url = productData.FindElement(By.ClassName("card__url")).GetAttribute("href");

        var productName = productData.FindElement(By.ClassName("card__name")).Text;
        var productPriceContainer = productData.FindElement(By.ClassName("price-tag"));
        var priceEur = productPriceContainer.FindElement(By.TagName("span")).Text;
        var priceCents = productPriceContainer.FindElement(By.TagName("sup")).Text;

        var product = new Product
        {
            Name = productName,
            Store = store,
            StoreId = store.Id,
            Price = decimal.Parse($"{priceEur}.{priceCents}"),
            ProductUrl = url
        };

        return product;
    }

    private void SetProductDetails(Product product, IWebDriver driver)
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
            SetProductDetails(product, driver);
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

            product.Coefficient = Math.Round(product.AlcContent * product.Amount / product.Price, 2);
        }
    }

    private void ClickCookieBanner(IWebDriver driver)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        // Agree with cookies
        try
        {
            var cookieButton = driver.FindElement(By.Id("CybotCookiebotDialogBodyLevelButtonLevelOptinDeclineAll"));
            wait.Until(_ => cookieButton.Displayed);
            cookieButton.Click();
        }
        catch (NoSuchElementException)
        {
            _logger.LogInformation("Didnt find the cookie element");
        }
    }
}
