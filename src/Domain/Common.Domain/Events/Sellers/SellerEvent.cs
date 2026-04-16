namespace Domain.Common.Events.Sellers;

public abstract record SellerEvent : DomainEvent
{
    public Guid SellerId { get; init; }
}
