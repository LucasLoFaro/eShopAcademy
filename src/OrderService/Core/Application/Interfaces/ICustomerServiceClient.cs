using Domain.Order.Entities;

namespace Core.Application.Interfaces;

public interface ICustomerServiceClient
{
    Task<Customer> GetCustomerByIdAsync(Guid customerId);
}