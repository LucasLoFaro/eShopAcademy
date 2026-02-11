using Domain.Customers.Entities;

namespace Customers.Infrastructure.Data;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> UpdateAsync(Guid id, Customer customer, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
