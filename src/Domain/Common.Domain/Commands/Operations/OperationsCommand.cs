namespace Common.Domain.Commands.Stock;

public record OperationsCommand : BaseCommand;

public record StartOrderPreparationCommand : OperationsCommand
{
    public string WarehouseId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
}