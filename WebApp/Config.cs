using System.Diagnostics;
using App.DAL.Repositories;
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
        services.AddScoped<IScrapeWorker, RimiWorker>();
        services.AddScoped<IScrapeWorker, SelverWorker>();
        services.AddScoped<IScrapeWorker, CoopWorker>();
    }

    public static void UseWorker(this WebApplication app, int maxParallelization)
    {
        using var scope = app.Services.CreateScope();
        var workers = scope.ServiceProvider.GetServices<IScrapeWorker>();
        Trace.Assert(workers is not null);
        
        foreach (var worker in workers) {
            
            RecurringJob.AddOrUpdate(
                worker.ToString(),
                () => worker.Work(maxParallelization),
                Cron.Daily);
            
        }
    }
}
