namespace Domain.Common.Commands.Orders;

public abstract record OrderCommand : BaseCommand
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}