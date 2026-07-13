namespace Domain.Common.Commands.Operations;

public record PreparePackageCommand : OperationsCommand
{
    public Guid ReservationId { get; init; }
    public Guid? SellerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public List<PreparePackageItem> Items { get; init; } = [];
}

public record PreparePackageItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}