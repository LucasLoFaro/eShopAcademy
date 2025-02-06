using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PaymentStatus Status { get; set; }
    public string ProviderTransactionId { get; set; }
    public double Amount { get; set; }
}
