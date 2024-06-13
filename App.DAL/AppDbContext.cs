using Microsoft.EntityFrameworkCore;
using App.Models;

namespace App.DAL;

public class AppDbContext: DbContext
{
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<Store> Stores { get; set; } = default!;
    
    public AppDbContext(DbContextOptions options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductUrl)
            .IsUnique();
    }
}