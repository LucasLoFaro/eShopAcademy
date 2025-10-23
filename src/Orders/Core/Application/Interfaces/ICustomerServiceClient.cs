using Domain.Orders.Entities;

namespace Core.Application.Interfaces;

public interface ICustomerServiceClient
{
    Task<Customer> GetCustomerByIdAsync(Guid customerId);
}