using System.Collections.Generic;

namespace Domain.Common.Commands.Basket;

public record ReinstateBasketCommand : BasketCommand
{
    public List<BasketItem> Items { get; init; } = new();
}

public class BasketItem
{
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
}
