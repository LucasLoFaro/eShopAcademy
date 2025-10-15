using Core.Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Product.Entities;
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
}
