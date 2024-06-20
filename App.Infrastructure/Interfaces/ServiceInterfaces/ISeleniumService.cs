using OpenQA.Selenium;

namespace App.Infrastructure.Interfaces.ServiceInterfaces;

public interface ISeleniumService
{
    public IWebDriver CreateDriver();
    
}