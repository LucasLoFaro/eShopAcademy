namespace Domain.Common.Commands.Orders;

public record FailOrderCommand : OrderCommand
{
    public string Stage { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
