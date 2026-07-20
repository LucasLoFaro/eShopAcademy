using Core.Application.Interfaces.Data;
using Domain.Products.Contracts;
using Domain.Products.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data.Repositories;

public class ProductsRepository : IProductsRepository
{
    private readonly IMongoCollection<Product> _products;

    public ProductsRepository(ProductDbContext context)
    {
        _products = context.Products;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _products.Find(Builders<Product>.Filter.Empty).ToListAsync(cancellationToken);

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _products.Find(Builders<Product>.Filter.Eq(p => p.Id, id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<Product?> GetMostExpensive(CancellationToken cancellationToken = default)
        => await _products
                .Find(Builders<Product>.Filter.Empty)
                .SortByDescending(p => p.Price)
                .FirstOrDefaultAsync(cancellationToken);

    public async Task AddOrUpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Id, product.Id);
        await _products.ReplaceOneAsync(filter, product, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }


    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _products.DeleteOneAsync(Builders<Product>.Filter.Eq(p => p.Id, product.Id), cancellationToken);
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default)
    {
        // MongoDB supports server-side querying, but the current filtering logic
        // is kept in memory to preserve the existing behavior.
        var products = await _products.Find(Builders<Product>.Filter.Empty).ToListAsync(cancellationToken);
        var results = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchText = filter.SearchText;
            results = results.Where(p =>
                p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            results = results.Where(p => p.Category != null && p.Category.Name.Contains(filter.Category, StringComparison.OrdinalIgnoreCase));

        if (filter.MinPrice.HasValue)
            results = results.Where(p => (p.DealPrice ?? p.Price) >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            results = results.Where(p => (p.DealPrice ?? p.Price) <= filter.MaxPrice.Value);

        if (filter.Deals == true)
            results = results.Where(p => p.IsDeal);

        if (filter.MinRating.HasValue)
            results = results.Where(p => p.Rating >= filter.MinRating.Value);

        var filtered = results.ToList();
        var totalCount = filtered.Count;

        IEnumerable<Product> sorted = filter.Sort switch
        {
            "price-asc" => filtered.OrderBy(p => p.DealPrice ?? p.Price),
            "price-desc" => filtered.OrderByDescending(p => p.DealPrice ?? p.Price),
            "rating" => filtered.OrderByDescending(p => p.Rating),
            "new" => filtered.OrderByDescending(p => p.CreatedAt),
            "best-sellers" => filtered.OrderByDescending(p => p.ReviewCount),
            "name-asc" => filtered.OrderBy(p => p.Name),
            "name-desc" => filtered.OrderByDescending(p => p.Name),
            _ => filtered.OrderByDescending(p => p.CreatedAt)
        };

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
