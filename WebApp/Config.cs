using System.Diagnostics;
using App.DAL.Repositories;
using App.Infrastructure.Interfaces;
using App.Infrastructure.Interfaces.ServiceInterfaces;
using App.Infrastructure.Services;
using App.Infrastructure.Workers;
using App.Models;
using Hangfire;

namespace WebApp;

public static class Config
{
    public static void AddConfiguredHangfire(this IServiceCollection services)
    {
        services.AddHangfire(conf =>
            conf.UseInMemoryStorage()
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
        );

        services.AddHangfireServer();
    }

    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepository<Product>, ProductRepository>();
        services.AddScoped<IRepository<Store>, StoreRepository>();
    }

    public static void AddWorkers(this IServiceCollection services)
    {
        services.AddScoped<ISeleniumService, SeleniumService>();
        services.AddScoped<IScrapeWorker, RimiWorker>();
    }

    public static void UseWorker(this WebApplication app, int maxParallelization)
    {
        using var scope = app.Services.CreateScope();
        var worker = scope.ServiceProvider.GetService<IScrapeWorker>();
        Trace.Assert(worker is not null);

        RecurringJob.AddOrUpdate(
            "scrape_sites",
            () => worker.Work(maxParallelization),
            Cron.Daily);
    }
}