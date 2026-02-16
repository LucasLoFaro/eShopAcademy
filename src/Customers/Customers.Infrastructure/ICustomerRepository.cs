using Domain.Customers.Entities;

namespace Customers.Infrastructure.Data;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> UpdateAsync(Guid id, Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    
    // Address management
    Task<SavedAddress> AddAddressAsync(Guid customerId, SavedAddress address, CancellationToken ct = default);
    Task<SavedAddress?> UpdateAddressAsync(Guid customerId, Guid addressId, SavedAddress address, CancellationToken ct = default);
    Task<bool> DeleteAddressAsync(Guid customerId, Guid addressId, CancellationToken ct = default);
    Task<bool> SetDefaultAddressAsync(Guid customerId, Guid addressId, CancellationToken ct = default);
}
