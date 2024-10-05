using App.Models;

namespace App.Infrastructure.Workers;

public interface IScrapeWorker
{
    public Task Work(int maxThreadCount);
}
