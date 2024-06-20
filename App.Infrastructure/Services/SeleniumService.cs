using App.DAL;
using App.Infrastructure.Interfaces;
using App.Infrastructure.Interfaces.ServiceInterfaces;
using App.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace App.Infrastructure.Services;

public class SeleniumService: ISeleniumService
{
    public IWebDriver CreateDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        return new FirefoxDriver(options);
    }

}