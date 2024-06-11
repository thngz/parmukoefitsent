using System.Diagnostics;
using App.DAL;
using App.Infrastructure.Interfaces;
using App.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace App.Infrastructure.Workers;

public class RimiUrl
{
    private int _currentPage = 1;
    private int _pageSize = 20;
    private const string BaseUrl = "https://www.rimi.ee/epood/ee/tooted/alkohol/c/SH-1?";

    public string Url
    {
        get
        {
            var newUrl = $"{BaseUrl}currentPage={_currentPage}&pageSize={_pageSize}";
            _currentPage++;
            return newUrl;
        }
    }
}

public class RimiWorker : IScrapeWorker
{
    private readonly RimiUrl _url = new();
    private readonly IWebDriver _driver;
    private readonly AppDbContext _context;
    private readonly ILogger<RimiWorker> _logger;
    private readonly Store _store;

    public RimiWorker(IWebDriver driver, AppDbContext context, ILogger<RimiWorker> logger)
    {
        _driver = driver;
        _context = context;
        _logger = logger;
        _store = _context.Stores.FirstOrDefault(s => s.Name == "Rimi");
        _driver.Navigate().GoToUrl(_url.Url);
        ClickCookieBanner();
    }

    public void Work()
    {
        var paginationList = _driver.FindElement(By.ClassName("pagination__list"));
        var paginationItems = paginationList.FindElements(By.ClassName("pagination__item"));
        var lastPage = int.Parse(paginationItems.ElementAt(paginationItems.Count - 2).Text);

        for (var i = 0; i < 1; i++)
        {
            _logger.LogInformation($"starting {i + 1}th page ");
            var products = GetProductsOnPage();

            foreach (var product in products)
            {
                _logger.LogInformation($"Visiting {product.ProductUrl}");
                _driver.Navigate().GoToUrl(product.ProductUrl);
                CalculateCoefficient(product);

                _logger.LogInformation($"{product.Name} object made");
                UpsertProductIntoDb(product);
            }

            _context.SaveChanges();
            _driver.Navigate().GoToUrl(_url.Url);
        }

        _context.SaveChanges();
    }


    public List<Product> GetProductsOnPage()
    {
        var products = new List<Product>();

        var productsOnPage = _driver.FindElements(By.CssSelector(".product-grid__item"));
        Debug.Assert(productsOnPage.Count > 0);

        foreach (var productData in productsOnPage)
        {
            var product = CreateInitialProduct(productData);
            products.Add(product);
        }

        return products;
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
            Price = decimal.Parse($"{priceEur}.{priceCents}"),
            ProductUrl = url
        };

        return product;
    }

    private void CalculateCoefficient(Product product)
    {
        var productDetails = _driver.FindElement(By.ClassName("list")).FindElements(By.ClassName("item"));

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


    private void UpsertProductIntoDb(Product product)
    {
        var existingProduct = _context.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.ProductUrl == product.ProductUrl);
        
        if (existingProduct is null)
        {
            _context.Add(product);
            _logger.LogInformation($"{product.Name} added to database");
        }
        else
        {
            product.Id = existingProduct.Id;
            _context.Products.Update(product);
            _logger.LogInformation($"{product.Name} updated in database");
        }
    }

    private void ClickCookieBanner()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        // Agree with cookies
        try
        {
            var cookieButton = _driver.FindElement(By.Id("CybotCookiebotDialogBodyLevelButtonLevelOptinDeclineAll"));
            wait.Until(d => cookieButton.Displayed);
            cookieButton.Click();
        }
        catch (NoSuchElementException e)
        {
            _logger.LogInformation("Didnt find the cookie element");
        }
    }
}