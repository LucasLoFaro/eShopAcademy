using Common.Domain.Commands.Stock;

namespace Common.Domain.Commands.Shipping;

public record ScheduleShippingCommand : ShippingCommand
{
    public string CustomerEmail { get; init; } = string.Empty;
    public string DestinationAddress { get; init; } = string.Empty;
}
