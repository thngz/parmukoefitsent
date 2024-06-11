using System.Configuration;
using App.DAL;
using App.Infrastructure;
using App.Infrastructure.Interfaces;
using App.Infrastructure.Workers;
using DAL;
using Hangfire;
using Hangfire.Server;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using WebApp;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString ??
                      throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));

builder.Services.AddHangfire(conf =>
    conf.UseInMemoryStorage()
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
);

builder.Services.AddHangfireServer();

var options = new FirefoxOptions();
options.AddArgument("-headless");

builder.Services.AddSingleton<IWebDriver>(new FirefoxDriver(options));
builder.Services.AddScoped<IScrapeWorker, RimiWorker>();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions()
{
    Authorization = new []
    {
        new HangfireCustomBasicAuthenticationFilter
        {
            User = builder.Configuration.GetValue<string>("HangFire:Username"),
            Pass = builder.Configuration.GetValue<string>("HangFire:Password")
        }
    }
});

app.UseWorker();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();