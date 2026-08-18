namespace Domain.Common.Events.Products;

public record ProductDeletedEvent : ProductEvent
{
    public const int CurrentContractVersion = 1;

    public int ContractVersion { get; init; } = CurrentContractVersion;
    public override Guid CorrelationId => ProductId;
    public override string EventType { get; set; } = "Deleted";
}
