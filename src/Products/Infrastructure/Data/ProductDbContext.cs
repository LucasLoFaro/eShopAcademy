using Microsoft.EntityFrameworkCore;
using Domain.Products.Entities;

namespace Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products => base.Set<Product>();

    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultContainer("eShopAcademy");

        modelBuilder.Entity<Product>().ToContainer("Products");
        modelBuilder.Entity<Product>().HasPartitionKey(p => p.CategoryId);

        modelBuilder.Entity<Product>().OwnsOne(p => p.Category);
        modelBuilder.Entity<Product>().OwnsMany(p => p.Specs);
        modelBuilder.Entity<Product>().OwnsMany(p => p.Faqs);
    }
}
