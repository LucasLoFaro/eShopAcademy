namespace Domain.Common.Events.Orders;

public sealed record OrderItemSellerAttribution
{
    public Guid OrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid SellerId { get; init; }
    public Guid SellerSaleOperationId { get; init; }
}
