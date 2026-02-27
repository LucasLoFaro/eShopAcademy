using Core.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Products.Contracts;
using Domain.Products.Entities;
using AutoMapper;


namespace Infrastructure.Data.Repositories;

public class ProductsRepository : IProductsRepository
{
    private readonly ProductDbContext _context;
    private readonly IMapper _mapper;

    public ProductsRepository(ProductDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
        => await _context.Products.ToListAsync();

    public async Task<Product?> GetByIdAsync(Guid id)
        => await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Product?> GetMostExpensive()
        => await _context.Products
                .OrderByDescending(p => p.Price)
                .FirstOrDefaultAsync();

    public async Task AddOrUpdateAsync(Product product)
    {
        var existingProduct = await GetByIdAsync(product.Id);

        if (existingProduct == null)
            _context.Products.Add(product);
        else
            _mapper.Map(product, existingProduct);

        await _context.SaveChangesAsync();
    }


    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter)
    {
        // Cosmos DB emulator has limited query translation, so we filter in memory.
        // This matches the pattern used by the existing GetAllAsync/Get endpoint.
        var products = await _context.Products.ToListAsync();
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
