using App.Infrastructure.Interfaces;

namespace WebApp;

public static class Config
{
    public static void UseWorker(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var worker = scope.ServiceProvider.GetService<IScrapeWorker>();
        worker.Work();
    }
}