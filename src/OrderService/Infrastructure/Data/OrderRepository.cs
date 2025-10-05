using Microsoft.EntityFrameworkCore;
using Core.Domain.Entities;


namespace Data;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db) => _db = db;

    public async Task AddAsync(Order order)
    {
        await _db.Orders.AddAsync(order);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Order>> GetAllAsync()
        => await _db.Orders
        .Include(o => o.Items)
        .ThenInclude(i => i.Product)
        .ToListAsync();

    public async Task<Order?> GetByIdAsync(Guid id)
        => await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
}
