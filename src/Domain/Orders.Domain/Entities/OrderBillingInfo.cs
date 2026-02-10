using Domain.Orders.Enums;

namespace Domain.Orders.Entities;

public class OrderBillingInfo
{
    public BillingStatus Status { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTime? BilledAt { get; set; }
}
