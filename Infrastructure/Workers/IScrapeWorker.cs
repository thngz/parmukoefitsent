using Models;

namespace Infrastructure.Workers;

public interface IScrapeWorker
{
    public Task Work(int maxThreadCount);
}
