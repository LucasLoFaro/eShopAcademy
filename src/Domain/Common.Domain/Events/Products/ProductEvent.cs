namespace Common.Domain.Events.Products;

public abstract record ProductEvent : DomainEvent
{
    public Guid ProductId { get; set; }
    public abstract string EventType { get; set; }
}