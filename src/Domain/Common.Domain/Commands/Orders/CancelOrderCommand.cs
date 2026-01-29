namespace Common.Domain.Commands.Orders;

public record CancelOrderCommand : OrderCommand
{
    public string Reason { get; init; } = string.Empty;
}
