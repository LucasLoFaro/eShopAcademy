using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public double Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public required string ProviderTransactionId { get; set; }
    public required string PaymentURL { get; set; }
}
