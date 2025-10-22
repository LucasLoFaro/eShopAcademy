using Microsoft.EntityFrameworkCore;
using Domain.Orders.Entities;


namespace Data;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;

    public OrderRepository(OrderDbContext db) => _db = db;

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
        => await _db.Orders
        .Include(o => o.Items)
        .ThenInclude(i => i.Product)
        .ToListAsync(ct);

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<bool> RemoveByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
            return false;

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
