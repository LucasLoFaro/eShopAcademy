using Domain.Orders.Entities;

namespace Domain.Orders.Contracts;

public class OrderRequest
{
    public Guid CustomerId { get; set; }
    public List<Item> Items { get; set; } = new();
}
