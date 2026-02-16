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

    // Address management
    public async Task<SavedAddress> AddAddressAsync(Guid customerId, SavedAddress address, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.Eq(c => c.Id, customerId);
        var customer = await _customers.Find(filter).FirstOrDefaultAsync(ct);
        
        if (customer is null)
            throw new InvalidOperationException($"Customer {customerId} not found");

        address.Id = Guid.NewGuid();
        address.CustomerId = customerId;
        address.CreatedAt = DateTime.UtcNow;
        address.ModifiedAt = DateTime.UtcNow;

        // If this is the first address or marked as default, set it as default
        if (customer.SavedAddresses.Count == 0 || address.IsDefault)
        {
            // Unset other defaults
            foreach (var addr in customer.SavedAddresses)
                addr.IsDefault = false;
            address.IsDefault = true;
        }

        var update = Builders<Customer>.Update
            .Push(c => c.SavedAddresses, address)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        await _customers.UpdateOneAsync(filter, update, cancellationToken: ct);
        return address;
    }

    public async Task<SavedAddress?> UpdateAddressAsync(Guid customerId, Guid addressId, SavedAddress updatedAddress, CancellationToken ct = default)
    {
        var filter = Builders<Customer>.Filter.And(
            Builders<Customer>.Filter.Eq(c => c.Id, customerId),
            Builders<Customer>.Filter.ElemMatch(c => c.SavedAddresses, a => a.Id == addressId)
        );

        var customer = await _customers.Find(Builders<Customer>.Filter.Eq(c => c.Id, customerId)).FirstOrDefaultAsync(ct);
        if (customer is null) return null;

        var existingAddress = customer.SavedAddresses.FirstOrDefault(a => a.Id == addressId);
        if (existingAddress is null) return null;

        updatedAddress.Id = addressId;
        updatedAddress.CustomerId = customerId;
        updatedAddress.CreatedAt = existingAddress.CreatedAt;
        updatedAddress.ModifiedAt = DateTime.UtcNow;

        // If setting as default, unset others
        if (updatedAddress.IsDefault)
        {
            foreach (var addr in customer.SavedAddresses.Where(a => a.Id != addressId))
                addr.IsDefault = false;
        }

        // Replace the address in the array
        var addressIndex = customer.SavedAddresses.FindIndex(a => a.Id == addressId);
        customer.SavedAddresses[addressIndex] = updatedAddress;

        var update = Builders<Customer>.Update
            .Set(c => c.SavedAddresses, customer.SavedAddresses)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        await _customers.UpdateOneAsync(Builders<Customer>.Filter.Eq(c => c.Id, customerId), update, cancellationToken: ct);
        return updatedAddress;
    }

    public async Task<bool> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        var customer = await _customers.Find(Builders<Customer>.Filter.Eq(c => c.Id, customerId)).FirstOrDefaultAsync(ct);
        if (customer is null) return false;

        var addressToDelete = customer.SavedAddresses.FirstOrDefault(a => a.Id == addressId);
        if (addressToDelete is null) return false;

        customer.SavedAddresses.RemoveAll(a => a.Id == addressId);

        // If deleted address was default and there are other addresses, set first one as default
        if (addressToDelete.IsDefault && customer.SavedAddresses.Count > 0)
        {
            customer.SavedAddresses[0].IsDefault = true;
        }

        var update = Builders<Customer>.Update
            .Set(c => c.SavedAddresses, customer.SavedAddresses)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        var result = await _customers.UpdateOneAsync(Builders<Customer>.Filter.Eq(c => c.Id, customerId), update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> SetDefaultAddressAsync(Guid customerId, Guid addressId, CancellationToken ct = default)
    {
        var customer = await _customers.Find(Builders<Customer>.Filter.Eq(c => c.Id, customerId)).FirstOrDefaultAsync(ct);
        if (customer is null) return false;

        var targetAddress = customer.SavedAddresses.FirstOrDefault(a => a.Id == addressId);
        if (targetAddress is null) return false;

        // Unset all defaults
        foreach (var addr in customer.SavedAddresses)
            addr.IsDefault = (addr.Id == addressId);

        var update = Builders<Customer>.Update
            .Set(c => c.SavedAddresses, customer.SavedAddresses)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        var result = await _customers.UpdateOneAsync(Builders<Customer>.Filter.Eq(c => c.Id, customerId), update, cancellationToken: ct);
        return result.ModifiedCount > 0;
    }
}
