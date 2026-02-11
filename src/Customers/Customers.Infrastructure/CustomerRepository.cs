using Domain.Customers.Entities;
using MongoDB.Driver;

namespace Customers.Infrastructure.Data;

public class CustomerRepository : ICustomerRepository
{
    private readonly IMongoCollection<Customer> _customers;

    public CustomerRepository(CustomerDbContext context)
    {
        _customers = context.Customers;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        return await _customers.Find(Builders<Customer>.Filter.Empty).ToListAsync(ct);
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.Id, id);
        return await _customers.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        customer.CreatedAt = now;
        customer.ModifiedAt = now;

        await _customers.InsertOneAsync(customer, cancellationToken: ct);
        return customer;
    }

    public async Task<Customer?> UpdateAsync(Guid id, Customer customer, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.Id, id);
        var existing = await _customers.Find(filter).FirstOrDefaultAsync(ct);

        if (existing is null)
            return null;

        customer.Id = id;
        customer.CreatedAt = existing.CreatedAt;
        customer.ModifiedAt = DateTime.UtcNow;

        await _customers.ReplaceOneAsync(filter, customer, cancellationToken: ct);
        return customer;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.Id, id);
        var result = await _customers.DeleteOneAsync(filter, ct);
        return result.DeletedCount > 0;
    }
}
