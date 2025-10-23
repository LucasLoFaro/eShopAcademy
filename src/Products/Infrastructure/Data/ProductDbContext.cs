using Microsoft.EntityFrameworkCore;
using Domain.Products.Entities;

namespace Infrastructure.Data;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products => base.Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultContainer("eShopAcademy");

        modelBuilder.Entity<Product>().ToContainer("Products");
        modelBuilder.Entity<Category>().ToContainer("Categories");

        modelBuilder.Entity<Product>().HasPartitionKey(p => p.CategoryId);
        modelBuilder.Entity<Category>().HasPartitionKey(c => c.Id);

        // Relationships
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
    }
}
