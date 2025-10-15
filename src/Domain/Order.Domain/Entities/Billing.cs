using Domain.Order.Enums;

namespace Domain.Order.Entities;

public class Billing
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public BillingStatus Status { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
}
