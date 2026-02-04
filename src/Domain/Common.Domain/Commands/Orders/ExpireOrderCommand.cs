namespace Domain.Common.Commands.Orders;

public record ExpireOrderCommand : OrderCommand
{
    public DateTime ExpiredAt { get; init; } = DateTime.UtcNow;
}
