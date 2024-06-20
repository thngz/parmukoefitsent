using App.DAL;
using App.Infrastructure.Interfaces;
using App.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace App.Infrastructure.Behaviors;

public class SeleniumBehavior: ISeleniumBehavior
{
    public IWebDriver CreateDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--headless");
        return new FirefoxDriver(options);
    }

}