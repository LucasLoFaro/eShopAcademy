using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public class OrderPaymentInfo
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? PaidAt { get; set; }
    
    // Compensation tracking
    public DateTime? ExpiredAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
