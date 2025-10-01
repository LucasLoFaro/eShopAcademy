using Core.Application.Interfaces.Services;
using Core.Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Data;

public static class SeedData
{
    public static async Task InitializeAsync(ProductDbContext context, IProductMessagingService messaging)
    {
        var productsExist = await context.Products.AsNoTracking().FirstOrDefaultAsync() != null;
        if (!productsExist)
        {
            var products = new List<Product>
            {
                new() { Id = new Guid("8b6e2e1d-6f1d-4b0d-9f6c-1a4c7b9d2e8f"), Name = "Logitech MX Master 3S", Price = 123.12, Description = "Best mouse ever", ImageUrl = "https://m.media-amazon.com/images/I/61ni3t1ryQL._AC_SL1500_.jpg", CategoryId = "a1507da2-eff7-4d99-878d-cc29a3e9eb57" },
                new() { Id = new Guid("c2d3f8a1-9b4d-41ea-b77f-5e20b39a48a6"), Name = "Logitech MX Keys Mini", Price = 158.4, Description = "Best keyboard ever", ImageUrl = "https://m.media-amazon.com/images/I/71lsfH+ww3L._UF894,1000_QL80_.jpg", CategoryId = "a1507da2-eff7-4d99-878d-cc29a3e9eb57" },
                new() { Id = new Guid("4f2a0b63-8e3d-4f7d-9b5c-1e19f9c3d21b"), Name = "SteelSeries Arctis Nova 7 Headset", Price = 320.58, Description = "Best wireless headset ever", ImageUrl = "https://m.media-amazon.com/images/I/61YjwXQ9U5L.jpg", CategoryId = "0a3e52e1-7e6a-4f9d-9249-72d4f6a3cb0e" }
            };
            context.Products.AddRange(products);

            var categories = new List<Category>
            {
                new() { Id = "5f914e7b-2e2e-4374-a1f4-3ddc64f8c245", Name = "Accesories"},
                new() { Id = "0a3e52e1-7e6a-4f9d-9249-72d4f6a3cb0e", Name = "Audio"}
            };
            context.Categories.AddRange(categories);
            
            await context.SaveChangesAsync();

            foreach (var product in products)
                await messaging.SendProductUpdate(product);
        }
    }
}