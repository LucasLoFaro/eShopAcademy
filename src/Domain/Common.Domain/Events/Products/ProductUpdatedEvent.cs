namespace Domain.Common.Events.Products;

public record ProductUpdatedEvent : ProductEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public override string EventType { get; set; } = "Updated";
}
