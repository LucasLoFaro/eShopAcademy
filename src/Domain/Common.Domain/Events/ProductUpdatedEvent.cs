namespace Domain.Common.Events;

public class ProductUpdatedEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public ProductEventType EventType { get; set; }
}
public enum ProductEventType
{
    Created,
    Updated,
    Deleted
}
