using Core.Domain.Entities;

namespace Core.Domain.Contracts;

public class OrderRequest
{
    public Guid CustomerId { get; set; }
    public List<Item> Items { get; set; } = new();
}
