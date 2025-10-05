using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public double Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public required string ProviderTransactionId { get; set; }
    public required string PaymentURL { get; set; }
}
