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
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchText = filter.SearchText.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchText) ||
                p.Description.ToLower().Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(p => p.Category != null && p.Category.Name.ToLower().Contains(filter.Category.ToLower()));

        if (filter.MinPrice.HasValue)
            query = query.Where(p => (p.DealPrice ?? p.Price) >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => (p.DealPrice ?? p.Price) <= filter.MaxPrice.Value);

        if (filter.Deals == true)
            query = query.Where(p => p.IsDeal);

        var totalCount = await query.CountAsync();

        query = filter.Sort switch
        {
            "price-asc" => query.OrderBy(p => p.DealPrice ?? p.Price),
            "price-desc" => query.OrderByDescending(p => p.DealPrice ?? p.Price),
            "rating" => query.OrderByDescending(p => p.Rating),
            "new" => query.OrderByDescending(p => p.CreatedAt),
            "best-sellers" => query.OrderByDescending(p => p.ReviewCount),
            "name-asc" => query.OrderBy(p => p.Name),
            "name-desc" => query.OrderByDescending(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
