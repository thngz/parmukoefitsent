using System.Diagnostics;
using App.Infrastructure.Behaviors;
using App.Infrastructure.Interfaces;
using App.Infrastructure.Workers;
using Hangfire;

namespace WebApp;

public static class Config
{
    public static void UseWorker(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var worker = scope.ServiceProvider.GetService<IScrapeWorker>();
        
        Debug.Assert(worker is not null);
        
        RecurringJob.AddOrUpdate(
            "scrape_sites",
            () => worker.Work(10),
            Cron.Daily);
    }

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

    public static void AddDefaultBehaviors(this IServiceCollection services)
    {
        services.AddScoped<ISeleniumBehavior, SeleniumBehavior>();
        services.AddScoped<IProductBehavior, ProductBehavior>();
    }

    public static void AddWorkers(this IServiceCollection services)
    {
        services.AddScoped<IScrapeWorker, RimiWorker>();
    }
}