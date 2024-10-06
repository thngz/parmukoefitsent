using System.Text.Json;
using System.Text.RegularExpressions;
using Models;
using Infrastructure.Repositories;

namespace Infrastructure.Workers;

public class Alcohol 
{
    public decimal Volume {get; set;}   
}

public class CoopProduct 
{
    public long Id2 {get; set;}
    public string Name {get; set;}
    public string Slug {get; set;}
    public decimal Price {get; set;}
    public Alcohol Alcohol {get; set;} 
}

public class CoopProducts 
{
    public List<CoopProduct> Data {get; set;}    
}

public class CoopWorker : IScrapeWorker
{
    private static HttpClient client = new() {
        BaseAddress = new Uri("https://api.vandra.ecoop.ee/supermarket/"),
    };
    
    private IRepository<Store> storeRepo;
    private IRepository<Product> productRepo;
    const string litrePattern = @"(\d+(\.\d+)?)L";
    private Store store; 
    
    public CoopWorker(IRepository<Store> storeRepository, IRepository<Product> productRepository)
    {
        storeRepo = storeRepository;
        productRepo = productRepository;

        store = storeRepo.GetEntity("Coop");
    }
    
    public async Task Work(int maxThreadCount) 
    {
        var categories = new List<int>() {54, 55, 56, 57, 58, 59, 88324, 214936};
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var products = new List<Product>();
         
        foreach (var category in categories) 
        {
            using HttpResponseMessage resp = await client.GetAsync($"products?category={category}&language=et&page=1");
            resp.EnsureSuccessStatusCode();
            var jsonResp = await resp.Content.ReadAsStringAsync();
            var coopProducts = JsonSerializer.Deserialize<CoopProducts>(jsonResp, options).Data;
            
            foreach (var coopProduct in coopProducts) 
            {
                Console.WriteLine(coopProduct.Name);
                decimal amount = 0; 
                foreach (Match match in Regex.Matches(coopProduct.Name, litrePattern))
                {
                    amount = decimal.Parse(match.Groups[1].Value);
                }
                    
                var product = new Product
                {
                    Name = coopProduct.Name,
                    Store = store,
                    StoreId = store.Id,
                    AlcContent = coopProduct.Alcohol.Volume,
                    Amount = amount,
                    Price = coopProduct.Price,
                    ProductUrl = $"https://vandra.ecoop.ee/et/toode/{coopProduct.Id2}-{coopProduct.Slug}",
                    Coefficient = Math.Round(coopProduct.Alcohol.Volume * amount / coopProduct.Price, 2)
                };
                products.Add(product);
            }
        }
        productRepo.UpsertEntities(products);
    }
}
