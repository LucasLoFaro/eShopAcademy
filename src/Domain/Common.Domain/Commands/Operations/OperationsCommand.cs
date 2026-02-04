namespace Domain.Common.Commands.Operations;

public record OperationsCommand : BaseCommand;

public record StartOrderPreparationCommand : OperationsCommand
{
    public string WarehouseId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
}