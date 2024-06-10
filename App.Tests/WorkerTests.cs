using App.Infrastructure.Workers;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace App.Tests;

public class WorkerTests
{
    [Fact]
    public void DriverWorks()
    {
        var options = new FirefoxOptions();
        options.AddArgument("-headless");
        var driver = new FirefoxDriver(options);
        // var worker = new RimiWorker(driver);
        // worker.Work();
        
    }
}