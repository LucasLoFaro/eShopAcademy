namespace Common.Domain.Events.Orders;

public abstract record OrderEvent : DomainEvent
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}