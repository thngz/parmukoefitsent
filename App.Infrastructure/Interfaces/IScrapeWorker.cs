using App.Models;

namespace App.Infrastructure.Interfaces;

public interface IScrapeWorker
{
    public Task WorkAsync();
}