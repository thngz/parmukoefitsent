using App.Infrastructure.Interfaces;
using Hangfire;

namespace WebApp;

public static class Config
{
    public static void UseWorker(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var worker = scope.ServiceProvider.GetService<IScrapeWorker>();
        RecurringJob.AddOrUpdate(
            "scrape_sites",
            () => worker.Work(),
            Cron.Minutely);
    }
}