namespace Common.Domain.Commands.Basket;

public record ReinstateBasketCommand : BasketCommand
{
    List<BasketItem> Items { get; init; } = new();
}

public class BasketItem
{
    public Guid ProductID { get; set; }
    public int Quantity { get; set; }
}