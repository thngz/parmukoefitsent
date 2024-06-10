using App.DAL;
using App.Infrastructure;
using App.Infrastructure.Interfaces;
using App.Infrastructure.Workers;
using DAL;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

var options = new FirefoxOptions();
options.AddArgument("-headless");

builder.Services.AddSingleton<IWebDriver>(new FirefoxDriver(options));
builder.Services.AddScoped<IScrapeWorker, RimiWorker>();

var app = builder.Build();

// app.UseWorker();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();