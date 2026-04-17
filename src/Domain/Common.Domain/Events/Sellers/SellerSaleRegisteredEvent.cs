namespace Domain.Common.Events.Sellers;

public record SellerSaleRegisteredEvent : SellerEvent
{
    public Guid OrderId { get; init; }
    public Guid OrderItemId { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal AccumulatedSalesAmount { get; init; }
    public decimal AccumulatedCommissionsAmount { get; init; }
}
