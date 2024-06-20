using App.DAL;
using App.Models;
using OpenQA.Selenium;

namespace App.Infrastructure.Interfaces;

public interface ISeleniumBehavior
{
    public IWebDriver CreateDriver();
    
}