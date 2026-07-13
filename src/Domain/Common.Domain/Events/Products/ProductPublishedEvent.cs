namespace Domain.Common.Events.Products;

public record ProductPublishedEvent : ProductEvent
{
    public override string EventType { get; set; } = "ProductPublished";
    public string Name { get; init; } = string.Empty;
    public double Price { get; init; }
    public Guid SellerId { get; init; }
}
