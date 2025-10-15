using Domain.Order.Entities;

namespace Domain.Order.Contracts;

public class OrderRequest
{
    public Guid CustomerId { get; set; }
    public List<Item> Items { get; set; } = new();
}
