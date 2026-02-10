using Domain.Orders.Entities;

namespace Core.Application.Interfaces;

public interface ICustomerServiceClient
{
    Task<OrderCustomerInfo> GetCustomerByIdAsync(Guid customerId);
}