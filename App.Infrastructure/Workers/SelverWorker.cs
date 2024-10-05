namespace App.Infrastructure.Workers;

public class SelverWorker: IScrapeWorker
{
    private static HttpClient client = new() 
    {
        BaseAddress = new Uri("https://www.example.com"),
    };
    
    public async Task Work(int maxThreadCount) 
    {
        Console.WriteLine("i run");
    }
}
