using System.Collections.Specialized;
using App.DAL;
using App.Models;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly AppDbContext _ctx;

    public IndexModel(ILogger<IndexModel> logger, AppDbContext ctx)
    {
        _logger = logger;
        _ctx = ctx;
    }

    public PaginatedList<Product> Products { get; set; }

    [BindProperty(SupportsGet = true)] public string Query { get; set; }
    [BindProperty(SupportsGet = true)] public string? SortValue { get; set; }

    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; }

    [BindProperty(SupportsGet = true)] public int PaginationSize { get; set; } = 10;
    
    public async Task<IActionResult> OnGet()
    {
        CurrentPage = CurrentPage == 0 ? 1 : CurrentPage;

        var products = from p in _ctx.Products
            join store in _ctx.Stores on p.Store equals store
            select new Product
            {
                Id = p.Id,
                Name = p.Name,
                Store = store,
                StoreId = store.Id,
                AlcContent = p.AlcContent,
                Amount = p.Amount,
                Price = p.Price,
                Coefficient = p.Coefficient,
                ProductUrl = p.ProductUrl
            };

        if (!string.IsNullOrEmpty(Query))
        {
            products = products.Where(s => s.Name.ToLower().Contains(Query.ToLower()));
        }

        var ordered = products.OrderByDescending(p => p.Coefficient);

        if (SortValue != null)
        {
            ordered = SortValue switch
            {
                "priceHighToLow" => products.OrderByDescending(p => p.Price),
                "priceLowToHigh" => products.OrderBy(p => p.Price),
                "coefficientHighToLow" => products.OrderByDescending(p => p.Coefficient),
                "coefficientLowToHigh" => products.OrderBy(p => p.Coefficient),
                "alcHighToLow" => products.OrderByDescending(p => p.AlcContent),
                "alcLowToHigh" => products.OrderBy(p => p.AlcContent),
                _ => ordered
            };
        }

        Products = await PaginatedList<Product>.CreateAsync(ordered.AsNoTracking(), CurrentPage, PaginationSize);

        if (!Request.IsHtmx())
            return Page();
        return Partial("_Products", this);
    }
    
}